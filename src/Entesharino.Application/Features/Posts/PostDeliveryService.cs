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
    private readonly IMediaStorage _mediaStorage;

    public PostDeliveryService(
        IDatabaseContext database,
        ISecretProtector secretProtector,
        IMessageSenderFactory senderFactory,
        IPostDeliveryRetryScheduler retryScheduler,
        IMediaStorage mediaStorage)
    {
        _database = database;
        _secretProtector = secretProtector;
        _senderFactory = senderFactory;
        _retryScheduler = retryScheduler;
        _mediaStorage = mediaStorage;
    }

    public async Task<ResultDto> RetryAsync(
        long postChannelId,
        CancellationToken cancellationToken = default)
    {
        var target = await _database.PostChannels
            .Include(x => x.Post)
                .ThenInclude(x => x.Media)
            .Include(x => x.Post)
                .ThenInclude(x => x.Schedule)
            .Include(x => x.Channel)
                .ThenInclude(x => x.Connection)
            .SingleOrDefaultAsync(
                x => x.Id == postChannelId && !x.IsRemoved,
                cancellationToken);

        if (target is null)
            return ResultDto.Fail("ارسال موردنظر پیدا نشد.", 404);

        if (target.Status == DeliveryStatus.Sent)
            return ResultDto.Ok("پیام قبلاً ارسال شده است.");

        if (target.Status == DeliveryStatus.Processing)
            return ResultDto.Ok("ارسال این کانال در حال انجام است.");

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

            var result = await SendPostAsync(
                sender,
                credentials,
                target,
                target.Post,
                cancellationToken);

            if (result.Success)
            {
                target.Status = DeliveryStatus.Sent;
                target.SentAt = DateTime.UtcNow;
                target.ExternalMessageId = result.ExternalMessageId;
                target.ErrorMessage = null;

                attempt.IsSuccess = true;
                attempt.CompletedAt = DateTime.UtcNow;

                await RecalculatePostStatusAsync(
                    target.Post,
                    cancellationToken);

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

    private async Task<SenderResult> SendPostAsync(
        IMessageSender sender,
        ChannelCredentials credentials,
        PostChannel target,
        Post post,
        CancellationToken cancellationToken)
    {
        var media = post.Media
            .Where(x => !x.IsRemoved)
            .OrderBy(x => x.Id)
            .ToList();

        if (media.Count == 0)
        {
            return await sender.SendTextAsync(
                credentials,
                post.Content!,
                cancellationToken);
        }

        var deliveries = await _database.PostMediaDeliveries
            .Where(x => x.PostChannelId == target.Id)
            .ToListAsync(cancellationToken);

        foreach (var item in media)
        {
            if (deliveries.All(x => x.PostMediaId != item.Id))
            {
                var delivery = new PostMediaDelivery
                {
                    PostChannelId = target.Id,
                    PostMediaId = item.Id,
                    Status = DeliveryStatus.Pending
                };

                _database.PostMediaDeliveries.Add(delivery);
                deliveries.Add(delivery);
            }
        }

        await _database.SaveChangesAsync(cancellationToken);

        SenderResult? lastResult = null;
        var captionUsed = deliveries.Any(x =>
            x.Status == DeliveryStatus.Sent &&
            media.Any(m => m.Id == x.PostMediaId));

        foreach (var item in media)
        {
            var delivery = deliveries.Single(x => x.PostMediaId == item.Id);

            if (delivery.Status == DeliveryStatus.Sent)
                continue;

            delivery.Status = DeliveryStatus.Processing;
            delivery.ErrorMessage = null;
            delivery.RetryCount++;

            try
            {
                await using var stream = await _mediaStorage.OpenReadAsync(
                    item.FileUrl,
                    cancellationToken);

                lastResult = await sender.SendMediaAsync(
                    credentials,
                    new MediaMessage(
                        item.MediaType,
                        stream,
                        item.FileName,
                        ResolveMediaContentType(item.MediaType, item.FileName),
                        captionUsed ? null : post.Content),
                    cancellationToken);

                if (!lastResult.Success)
                {
                    delivery.Status = DeliveryStatus.Failed;
                    delivery.ErrorMessage = lastResult.ErrorMessage;
                    await _database.SaveChangesAsync(cancellationToken);
                    return lastResult;
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is HttpRequestException ||
                ex is InvalidOperationException)
            {
                delivery.Status = DeliveryStatus.Failed;
                delivery.ErrorMessage = ex.Message;
                await _database.SaveChangesAsync(cancellationToken);
                throw;
            }

            delivery.Status = DeliveryStatus.Sent;
            delivery.SentAt = DateTime.UtcNow;
            delivery.ExternalMessageId = lastResult.ExternalMessageId;
            delivery.ErrorMessage = null;
            captionUsed = true;

            await _database.SaveChangesAsync(cancellationToken);
        }

        return lastResult ?? new SenderResult(true);
    }

    private static string ResolveMediaContentType(
        MediaType mediaType,
        string fileName)
    {
        return mediaType switch
        {
            MediaType.Image => GetMimeType(fileName, "image/jpeg"),
            MediaType.Video => GetMimeType(fileName, "video/mp4"),
            MediaType.Audio => GetMimeType(fileName, "audio/mpeg"),
            MediaType.Document => GetMimeType(fileName, "application/octet-stream"),
            _ => "application/octet-stream"
        };
    }

    private static string GetMimeType(string fileName, string fallback)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".zip" => "application/zip",
            _ => fallback
        };
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

        await RecalculatePostStatusAsync(
            target.Post,
            cancellationToken);

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

    private async Task RecalculatePostStatusAsync(
        Post post,
        CancellationToken cancellationToken)
    {
        var channels = await _database.PostChannels
            .Where(x => x.PostId == post.Id && !x.IsRemoved)
            .Select(x => new
            {
                x.Status,
                x.RetryCount
            })
            .ToListAsync(cancellationToken);

        if (channels.Count == 0)
        {
            post.Status = PostStatus.Failed;
            await _database.SaveChangesAsync(cancellationToken);
            return;
        }

        var sent = channels.Count(x => x.Status == DeliveryStatus.Sent);
        var failed = channels.Count(x => x.Status == DeliveryStatus.Failed);
        var processing = channels.Count(x => x.Status == DeliveryStatus.Processing);
        var pending = channels.Count(x => x.Status == DeliveryStatus.Pending);
        var hasRetryableFailure = channels.Any(x =>
            x.Status == DeliveryStatus.Failed &&
            x.RetryCount < MaxRetries);

        var isRecurring = post.Schedule is not null &&
            post.Schedule.ScheduleType is
                ScheduleType.Daily or
                ScheduleType.Weekly or
                ScheduleType.Monthly or
                ScheduleType.Cron &&
            !post.Schedule.IsCompleted;

        post.Status = isRecurring
            ? PostStatus.Scheduled
            : sent == channels.Count
                ? PostStatus.Completed
                : sent > 0
                    ? PostStatus.PartiallyCompleted
                    : processing > 0 || pending > 0 || hasRetryableFailure
                        ? PostStatus.Processing
                        : failed == channels.Count
                            ? PostStatus.Failed
                            : PostStatus.Failed;

        await _database.SaveChangesAsync(cancellationToken);
    }

    private async Task<ResultDto> FailPermanentlyAsync(
        PostChannel target,
        string error,
        CancellationToken cancellationToken)
    {
        target.Status = DeliveryStatus.Failed;
        target.ErrorMessage = error;

        await RecalculatePostStatusAsync(
            target.Post,
            cancellationToken);

        return ResultDto.Fail(error, 400);
    }
}
