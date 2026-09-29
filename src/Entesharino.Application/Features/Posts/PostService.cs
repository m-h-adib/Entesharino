using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Posts.Models;
using Entesharino.Domain.Entities;
using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Posts;

public sealed class PostService : IPostService
{
    private readonly IDatabaseContext _database;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecretProtector _secretProtector;
    private readonly IMessageSenderFactory _senderFactory;
    private readonly IPostScheduler _postScheduler;

    public PostService(
        IDatabaseContext database,
        ICurrentUserService currentUser,
        ISecretProtector secretProtector,
        IMessageSenderFactory senderFactory,
        IPostScheduler postScheduler)
    {
        _database = database;
        _currentUser = currentUser;
        _secretProtector = secretProtector;
        _senderFactory = senderFactory;
        _postScheduler = postScheduler;
    }

    public async Task<ResultOfList<PostListItemDto>> GetListAsync(
        PostListRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultOfList<PostListItemDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.Search?.Trim();

        var query = _database.Posts
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsRemoved);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Title.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PostListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                ChannelCount = x.Channels.Count(c => !c.IsRemoved),
                SentChannelCount = x.Channels.Count(c =>
                    !c.IsRemoved && c.Status == DeliveryStatus.Sent),
                ScheduledAt = x.Schedule != null
                    ? x.Schedule.ScheduledAt
                    : null
            })
            .ToListAsync(cancellationToken);

        return ResultOfList<PostListItemDto>.Ok(data, totalCount);
    }

    public async Task<ResultDto<PostDetailsDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<PostDetailsDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var post = await _database.Posts
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved)
            .Select(x => new PostDetailsDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                Channels = x.Channels
                    .Where(c => !c.IsRemoved)
                    .Select(c => new PostChannelDto
                    {
                        ChannelId = c.ChannelId,
                        ChannelName = c.Channel.Name,
                        Platform = c.Channel.Platform,
                        Status = c.Status,
                        ScheduledAt = c.ScheduledAt,
                        SentAt = c.SentAt,
                        ExternalMessageId = c.ExternalMessageId,
                        ErrorMessage = c.ErrorMessage,
                        RetryCount = c.RetryCount
                    })
                    .ToList(),
                Schedule = x.Schedule == null
                    ? null
                    : new PostScheduleDto
                    {
                        ScheduleType = x.Schedule.ScheduleType,
                        ScheduledAt = x.Schedule.ScheduledAt,
                        CronExpression = x.Schedule.CronExpression,
                        TimeZone = x.Schedule.TimeZone,
                        NextRunAt = x.Schedule.NextRunAt,
                        IsCompleted = x.Schedule.IsCompleted
                    }
            })
            .SingleOrDefaultAsync(cancellationToken);

        return post is null
            ? ResultDto<PostDetailsDto>.Fail("پست موردنظر پیدا نشد.", 404)
            : ResultDto<PostDetailsDto>.Ok(post);
    }

    public async Task<ResultDto<PostDetailsDto>> CreateAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<PostDetailsDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var validation = ValidatePostInput(request.Title, request.Content);
        if (validation is not null)
            return ResultDto<PostDetailsDto>.Fail(validation, 400);

        var channelIds = request.ChannelIds.Distinct().ToList();
        if (channelIds.Count == 0)
            return ResultDto<PostDetailsDto>.Fail(
                "حداقل یک کانال برای پست انتخاب کنید.", 400);

        var channels = await _database.Channels
            .Where(x =>
                channelIds.Contains(x.Id) &&
                x.UserId == userId.Value &&
                !x.IsRemoved &&
                x.IsActive)
            .ToListAsync(cancellationToken);

        if (channels.Count != channelIds.Count)
            return ResultDto<PostDetailsDto>.Fail(
                "یک یا چند کانال انتخاب‌شده معتبر، فعال یا متعلق به کاربر جاری نیست.", 400);

        var post = new Post
        {
            UserId = userId.Value,
            Title = request.Title.Trim(),
            Content = string.IsNullOrWhiteSpace(request.Content)
                ? null
                : request.Content.Trim(),
            Status = PostStatus.Draft
        };

        foreach (var channel in channels)
        {
            post.Channels.Add(new PostChannel
            {
                ChannelId = channel.Id,
                Status = DeliveryStatus.Pending
            });
        }

        _database.Posts.Add(post);
        await _database.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(post.Id, cancellationToken);
    }

    public async Task<ResultDto<PostDetailsDto>> UpdateAsync(
        long id,
        UpdatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<PostDetailsDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var validation = ValidatePostInput(request.Title, request.Content);
        if (validation is not null)
            return ResultDto<PostDetailsDto>.Fail(validation, 400);

        var post = await _database.Posts
            .Include(x => x.Channels)
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved,
                cancellationToken);

        if (post is null)
            return ResultDto<PostDetailsDto>.Fail("پست موردنظر پیدا نشد.", 404);

        if (post.Status is PostStatus.Processing or PostStatus.Completed)
            return ResultDto<PostDetailsDto>.Fail(
                "پست در این وضعیت قابل ویرایش نیست.", 409);

        var channelIds = request.ChannelIds.Distinct().ToList();
        if (channelIds.Count == 0)
            return ResultDto<PostDetailsDto>.Fail(
                "حداقل یک کانال برای پست انتخاب کنید.", 400);

        var channels = await _database.Channels
            .Where(x =>
                channelIds.Contains(x.Id) &&
                x.UserId == userId.Value &&
                !x.IsRemoved &&
                x.IsActive)
            .ToListAsync(cancellationToken);

        if (channels.Count != channelIds.Count)
            return ResultDto<PostDetailsDto>.Fail(
                "یک یا چند کانال انتخاب‌شده معتبر، فعال یا متعلق به کاربر جاری نیست.", 400);

        post.Title = request.Title.Trim();
        post.Content = string.IsNullOrWhiteSpace(request.Content)
            ? null
            : request.Content.Trim();

        foreach (var existing in post.Channels.Where(x => !channelIds.Contains(x.ChannelId)))
            existing.IsRemoved = true;

        foreach (var channelId in channelIds)
        {
            var existing = post.Channels
                .FirstOrDefault(x => x.ChannelId == channelId && !x.IsRemoved);

            if (existing is null)
            {
                post.Channels.Add(new PostChannel
                {
                    ChannelId = channelId,
                    Status = DeliveryStatus.Pending
                });
            }
        }

        post.Status = PostStatus.Draft;

        await _database.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ResultDto> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto.Fail("کاربر جاری شناسایی نشد.", 401);

        var post = await _database.Posts
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved,
                cancellationToken);

        if (post is null)
            return ResultDto.Fail("پست موردنظر پیدا نشد.", 404);

        if (post.Status == PostStatus.Processing)
            return ResultDto.Fail("پست در حال ارسال است و فعلاً قابل حذف نیست.", 409);

        post.IsRemoved = true;
        post.IsActive = false;
        post.Status = PostStatus.Cancelled;

        await _database.SaveChangesAsync(cancellationToken);
        await _postScheduler.RemoveAsync(post.Id, cancellationToken);

        return ResultDto.Ok("پست با موفقیت حذف شد.");
    }

    public async Task<ResultDto> PublishAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto.Fail("کاربر جاری شناسایی نشد.", 401);

        return await PublishInternalAsync(id, userId.Value, cancellationToken);
    }

    public async Task<ResultDto> ExecuteScheduledAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var post = await _database.Posts
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsRemoved)
            .Select(x => new { x.Id, x.UserId, x.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (post is null)
            return ResultDto.Fail("پست زمان‌بندی‌شده پیدا نشد.", 404);

        if (post.Status != PostStatus.Scheduled)
            return ResultDto.Ok("پست در وضعیت قابل اجرای زمان‌بندی نیست.");

        return await PublishInternalAsync(post.Id, post.UserId, cancellationToken);
    }

    private async Task<ResultDto> PublishInternalAsync(
        long id,
        long ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var post = await _database.Posts
            .Include(x => x.Channels)
                .ThenInclude(x => x.Channel)
                    .ThenInclude(x => x.Connection)
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == ownerUserId && !x.IsRemoved,
                cancellationToken);

        if (post is null)
            return ResultDto.Fail("پست موردنظر پیدا نشد.", 404);

        if (string.IsNullOrWhiteSpace(post.Content))
            return ResultDto.Fail("متن پست خالی است.", 400);

        var targets = post.Channels
            .Where(x => !x.IsRemoved && x.Channel.IsActive)
            .ToList();

        if (targets.Count == 0)
            return ResultDto.Fail("هیچ کانال فعال و معتبری برای ارسال وجود ندارد.", 400);

        post.Status = PostStatus.Processing;
        await _database.SaveChangesAsync(cancellationToken);

        foreach (var target in targets)
        {
            if (target.Status == DeliveryStatus.Sent)
                continue;

            var attempt = new DeliveryAttempt
            {
                PostChannelId = target.Id,
                AttemptNumber = target.RetryCount + 1,
                StartedAt = DateTime.UtcNow
            };

            target.Status = DeliveryStatus.Processing;
            target.ErrorMessage = null;
            target.RetryCount++;
            _database.DeliveryAttempts.Add(attempt);

            try
            {
                if (target.Channel.Connection is null)
                    throw new InvalidOperationException("اطلاعات اتصال کانال ثبت نشده است.");

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
                    post.Content,
                    cancellationToken);

                if (result.Success)
                {
                    target.Status = DeliveryStatus.Sent;
                    target.SentAt = DateTime.UtcNow;
                    target.ExternalMessageId = result.ExternalMessageId;
                    target.ErrorMessage = null;

                    attempt.IsSuccess = true;
                }
                else
                {
                    target.Status = DeliveryStatus.Failed;
                    target.ErrorMessage = result.ErrorMessage;
                    attempt.IsSuccess = false;
                    attempt.ErrorMessage = result.ErrorMessage;
                }
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is System.Security.Cryptography.CryptographicException ||
                ex is HttpRequestException)
            {
                target.Status = DeliveryStatus.Failed;
                target.ErrorMessage = ex.Message;
                attempt.IsSuccess = false;
                attempt.ErrorMessage = ex.Message;
            }

            attempt.CompletedAt = DateTime.UtcNow;
            await _database.SaveChangesAsync(cancellationToken);
        }

        var finalTargets = await _database.PostChannels
            .Where(x => x.PostId == post.Id && !x.IsRemoved)
            .ToListAsync(cancellationToken);

        var sent = finalTargets.Count(x => x.Status == DeliveryStatus.Sent);
        var failed = finalTargets.Count(x => x.Status == DeliveryStatus.Failed);

        post.Status = sent == finalTargets.Count
            ? PostStatus.Completed
            : sent > 0
                ? PostStatus.PartiallyCompleted
                : failed == finalTargets.Count
                    ? PostStatus.Failed
                    : PostStatus.Processing;

        await _database.SaveChangesAsync(cancellationToken);

        return post.Status switch
        {
            PostStatus.Completed => ResultDto.Ok("پست با موفقیت به همه کانال‌ها ارسال شد."),
            PostStatus.PartiallyCompleted => ResultDto.Fail(
                "پست فقط به بخشی از کانال‌ها ارسال شد.", 207),
            _ => ResultDto.Fail(
                "ارسال پست به کانال‌ها ناموفق بود.", 400)
        };
    }

    public async Task<ResultDto> ScheduleAsync(
        long id,
        SchedulePostRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto.Fail("کاربر جاری شناسایی نشد.", 401);

        var post = await _database.Posts
            .Include(x => x.Schedule)
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved,
                cancellationToken);

        if (post is null)
            return ResultDto.Fail("پست موردنظر پیدا نشد.", 404);

        if (post.Status is PostStatus.Processing or PostStatus.Completed)
            return ResultDto.Fail(
                "پست در این وضعیت قابل زمان‌بندی نیست.", 409);

        if (request.ScheduleType == ScheduleType.Immediate)
            return ResultDto.Fail(
                "برای ارسال فوری از endpoint انتشار استفاده کنید.", 400);

        if (request.ScheduleType == ScheduleType.Cron &&
            string.IsNullOrWhiteSpace(request.CronExpression))
            return ResultDto.Fail(
                "برای زمان‌بندی Cron باید CronExpression وارد شود.", 400);

        if (request.ScheduleType != ScheduleType.Cron &&
            !request.ScheduledAt.HasValue)
            return ResultDto.Fail(
                "زمان اجرای زمان‌بندی الزامی است.", 400);

        if (request.ScheduledAt.HasValue &&
            request.ScheduledAt.Value <= DateTime.UtcNow)
            return ResultDto.Fail(
                "زمان زمان‌بندی باید در آینده باشد.", 400);

        post.Schedule ??= new PostSchedule();

        post.Schedule.ScheduleType = request.ScheduleType;
        post.Schedule.ScheduledAt = request.ScheduledAt;
        post.Schedule.CronExpression = string.IsNullOrWhiteSpace(request.CronExpression)
            ? null
            : request.CronExpression.Trim();
        post.Schedule.TimeZone = string.IsNullOrWhiteSpace(request.TimeZone)
            ? "UTC"
            : request.TimeZone.Trim();
        post.Schedule.NextRunAt = request.ScheduledAt;
        post.Schedule.LastRunAt = null;
        post.Schedule.IsCompleted = false;

        foreach (var channel in post.Channels.Where(x => !x.IsRemoved))
        {
            channel.Status = DeliveryStatus.Pending;
            channel.ScheduledAt = request.ScheduledAt;
            channel.SentAt = null;
            channel.ExternalMessageId = null;
            channel.ErrorMessage = null;
        }

        post.Status = PostStatus.Scheduled;

        await _database.SaveChangesAsync(cancellationToken);

        await _postScheduler.ScheduleAsync(
            post.Id,
            request.ScheduleType,
            request.ScheduledAt,
            post.Schedule.CronExpression,
            post.Schedule.TimeZone,
            cancellationToken);

        return ResultDto.Ok("پست با موفقیت زمان‌بندی شد.");
    }

    private static string? ValidatePostInput(string title, string? content)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "عنوان پست الزامی است.";

        if (title.Trim().Length > 300)
            return "عنوان پست نمی‌تواند بیشتر از 300 کاراکتر باشد.";

        if (string.IsNullOrWhiteSpace(content))
            return "متن پست الزامی است.";

        return null;
    }
}
