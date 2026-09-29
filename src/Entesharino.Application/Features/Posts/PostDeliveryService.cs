using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Domain.Entities;
using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Posts;

public sealed class PostDeliveryService : IPostDeliveryService
{
    private const int MaxRetries = 5;

    private readonly IDatabaseContext _database;
    private readonly ISecretProtector _secretProtector;
    private readonly IMessageSenderFactory _senderFactory;
    private readonly IPostDeliveryRetryScheduler _retryScheduler;

    public PostDeliveryService(
        IDatabaseContext database,
        ISecretProtector secretProtector,
        IMessageSenderFactory senderFactory,
        IPostDeliveryRetryScheduler retryScheduler)
    {
        _database = database;
        _secretProtector = secretProtector;
        _senderFactory = senderFactory;
        _retryScheduler = retryScheduler;
    }

    public async Task<ResultDto> RetryAsync(
        long postChannelId,
        CancellationToken cancellationToken = default)
    {
        var target = await _database.PostChannels
            .Include(x => x.Post)
            .Include(x => x.Channel)
                .ThenInclude(x => x.Connection)
            .SingleOrDefaultAsync(
                x => x.Id == postChannelId && !x.IsRemoved,
                cancellationToken);

        if (target is null)
            return ResultDto.Fail("ارسال موردنظر پیدا نشد.", 404);

        if (target.Status == DeliveryStatus.Sent)
            return ResultDto.Ok("پیام قبلاً ارسال شده است.");

        if (target.Post.IsRemoved || target.Post.Status == PostStatus.Cancelled)
            return ResultDto.Ok("پست دیگر قابل ارسال نیست.");

        if (string.IsNullOrWhiteSpace(target.Post.Content))
            return ResultDto.Fail("متن پست خالی است.", 400);

        if (!target.Channel.IsActive)
            return await FailPermanentlyAsync(
                target,
                "کانال غیرفعال است.",
                cancellationToken);

        if (target.RetryCount >= MaxRetries)
            return await FailPermanentlyAsync(
                target,
                "حداکثر تعداد تلاش برای ارسال انجام شده است.",
                cancellationToken);

        target.Status = DeliveryStatus.Processing;
        target.ErrorMessage = null;

        var attempt = new DeliveryAttempt
        {
            PostChannelId = target.Id,
            AttemptNumber = target.RetryCount + 1,
            StartedAt = DateTime.UtcNow
        };

        target.RetryCount++;
        _database.DeliveryAttempts.Add(attempt);
        await _database.SaveChangesAsync(cancellationToken);

        try
        {
            if (target.Channel.Connection is null)
                throw new InvalidOperationException(
                    "اطلاعات اتصال کانال ثبت نشده است.");

            var credentials = new ChannelCredentials(
                _secretProtector.Unprotect(
                    target.Channel.Connection.EncryptedAccessToken),
                target.Channel.Identifier,
                string.IsNullOrWhiteSpace(
                    target.Channel.Connection.EncryptedRefreshToken)
                    ? null
                    : _secretProtector.Unprotect(
                        target.Channel.Connection.EncryptedRefreshToken));

            var sender = _senderFactory.Get(target.Channel.Platform);

            var result = await sender.SendTextAsync(
                credentials,
                target.Post.Content,
                cancellationToken);

            if (result.Success)
            {
                target.Status = DeliveryStatus.Sent;
                target.SentAt = DateTime.UtcNow;
                target.ExternalMessageId = result.ExternalMessageId;
                target.ErrorMessage = null;

                attempt.IsSuccess = true;
                attempt.CompletedAt = DateTime.UtcNow;

                await _database.SaveChangesAsync(cancellationToken);
                return ResultDto.Ok("پیام با موفقیت ارسال شد.");
            }

            return await HandleFailureAsync(
                target,
                attempt,
                result.ErrorMessage ?? "ارسال پیام ناموفق بود.",
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is InvalidOperationException ||
            ex is System.Security.Cryptography.CryptographicException ||
            ex is HttpRequestException)
        {
            return await HandleFailureAsync(
                target,
                attempt,
                ex.Message,
                cancellationToken);
        }
    }

    private async Task<ResultDto> HandleFailureAsync(
        PostChannel target,
        DeliveryAttempt attempt,
        string error,
        CancellationToken cancellationToken)
    {
        target.Status = DeliveryStatus.Failed;
        target.ErrorMessage = error;

        attempt.IsSuccess = false;
        attempt.ErrorMessage = error;
        attempt.CompletedAt = DateTime.UtcNow;

        await _database.SaveChangesAsync(cancellationToken);

        if (target.RetryCount < MaxRetries)
        {
            await _retryScheduler.ScheduleAsync(
                target.Id,
                target.RetryCount,
                cancellationToken);

            return ResultDto.Fail(
                $"ارسال ناموفق بود و تلاش مجدد شماره {target.RetryCount + 1} زمان‌بندی شد.",
                202);
        }

        return ResultDto.Fail(
            "ارسال ناموفق بود و حداکثر تعداد تلاش انجام شد.",
            400);
    }

    private async Task<ResultDto> FailPermanentlyAsync(
        PostChannel target,
        string error,
        CancellationToken cancellationToken)
    {
        target.Status = DeliveryStatus.Failed;
        target.ErrorMessage = error;

        await _database.SaveChangesAsync(cancellationToken);

        return ResultDto.Fail(error, 400);
    }
}
