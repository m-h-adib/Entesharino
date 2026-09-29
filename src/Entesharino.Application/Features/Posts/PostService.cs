using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Posts.Models;
using Entesharino.Domain.Entities;
using Entesharino.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Entesharino.Application.Features.Posts;

public sealed class PostService : IPostService
{
    private readonly IDatabaseContext _database;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecretProtector _secretProtector;
    private readonly IMessageSenderFactory _senderFactory;
    private readonly IPostScheduler _postScheduler;
    private readonly IPostDeliveryRetryScheduler _retryScheduler;
    private readonly IMediaStorage _mediaStorage;

    public PostService(
        IDatabaseContext database,
        ICurrentUserService currentUser,
        ISecretProtector secretProtector,
        IMessageSenderFactory senderFactory,
        IPostScheduler postScheduler,
        IPostDeliveryRetryScheduler retryScheduler,
        IMediaStorage mediaStorage)
    {
        _database = database;
        _currentUser = currentUser;
        _secretProtector = secretProtector;
        _senderFactory = senderFactory;
        _postScheduler = postScheduler;
        _retryScheduler = retryScheduler;
        _mediaStorage = mediaStorage;
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
            .Include(x => x.Channels)
                .ThenInclude(x => x.Channel)
            .Include(x => x.Media)
            .SingleOrDefaultAsync(cancellationToken);

        if (post is null)
            return ResultDto<PostDetailsDto>.Fail(
                "پست موردنظر پیدا نشد.", 404);

        var channels = post.Channels
            .Where(x => !x.IsRemoved)
            .OrderBy(x => x.Id)
            .ToList();

        var media = post.Media
            .Where(x => !x.IsRemoved)
            .OrderBy(x => x.Id)
            .ToList();

        var deliveries = await _database.PostMediaDeliveries
            .AsNoTracking()
            .Where(x =>
                !x.IsRemoved &&
                channels.Select(c => c.Id).Contains(x.PostChannelId) &&
                media.Select(m => m.Id).Contains(x.PostMediaId))
            .ToListAsync(cancellationToken);

        var deliveryMap = deliveries
            .GroupBy(x => new { x.PostChannelId, x.PostMediaId })
            .ToDictionary(
                x => (x.Key.PostChannelId, x.Key.PostMediaId),
                x => x.OrderByDescending(d => d.Id).First());

        var mediaDtos = media
            .Select(item => new PostMediaDto
            {
                Id = item.Id,
                MediaType = item.MediaType,
                FileName = item.FileName,
                FileUrl = item.FileUrl,
                FileSize = item.FileSize,
                Deliveries = channels
                    .Select(channel =>
                    {
                        deliveryMap.TryGetValue(
                            (channel.Id, item.Id),
                            out var delivery);

                        return new PostMediaDeliveryItemDto
                        {
                            MediaId = item.Id,
                            FileName = item.FileName,
                            MediaType = item.MediaType,
                            FileSize = item.FileSize,
                            Status = delivery?.Status ?? DeliveryStatus.Pending,
                            SentAt = delivery?.SentAt,
                            ExternalMessageId = delivery?.ExternalMessageId,
                            ErrorMessage = delivery?.ErrorMessage,
                            RetryCount = delivery?.RetryCount ?? 0
                        };
                    })
                    .ToList()
            })
            .ToList();

        var details = new PostDetailsDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Status = post.Status,
            CreatedAt = post.CreatedAt,
            Channels = channels
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
            Media = mediaDtos,
            Schedule = post.Schedule == null
                ? null
                : new PostScheduleDto
                {
                    ScheduleType = post.Schedule.ScheduleType,
                    ScheduledAt = post.Schedule.ScheduledAt,
                    CronExpression = post.Schedule.CronExpression,
                    TimeZone = post.Schedule.TimeZone,
                    NextRunAt = post.Schedule.NextRunAt,
                    IsCompleted = post.Schedule.IsCompleted
                }
        };

        return ResultDto<PostDetailsDto>.Ok(details);
    }

    public async Task<ResultDto<PostMediaDeliveryReportDto>> GetMediaDeliveryReportAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<PostMediaDeliveryReportDto>.Fail(
                "کاربر جاری شناسایی نشد.", 401);

        var post = await _database.Posts
            .AsNoTracking()
            .Include(x => x.Channels)
                .ThenInclude(x => x.Channel)
            .Include(x => x.Media)
            .SingleOrDefaultAsync(
                x => x.Id == id &&
                     x.UserId == userId.Value &&
                     !x.IsRemoved,
                cancellationToken);

        if (post is null)
            return ResultDto<PostMediaDeliveryReportDto>.Fail(
                "پست موردنظر پیدا نشد.", 404);

        var channels = post.Channels
            .Where(x => !x.IsRemoved && !x.Channel.IsRemoved)
            .OrderBy(x => x.Id)
            .ToList();

        var media = post.Media
            .Where(x => !x.IsRemoved)
            .OrderBy(x => x.Id)
            .ToList();

        var deliveries = await _database.PostMediaDeliveries
            .AsNoTracking()
            .Where(x =>
                !x.IsRemoved &&
                x.PostChannel.PostId == post.Id &&
                !x.PostChannel.IsRemoved &&
                media.Select(m => m.Id).Contains(x.PostMediaId))
            .ToListAsync(cancellationToken);

        var deliveryMap = deliveries
            .GroupBy(x => new { x.PostChannelId, x.PostMediaId })
            .ToDictionary(
                x => (x.Key.PostChannelId, x.Key.PostMediaId),
                x => x.OrderByDescending(d => d.Id).First());

        var channelReports = channels
            .Select(channel => new PostMediaDeliveryChannelDto
            {
                ChannelId = channel.ChannelId,
                ChannelName = channel.Channel.Name,
                Platform = channel.Channel.Platform,
                ChannelStatus = channel.Status,
                ChannelSentAt = channel.SentAt,
                ChannelErrorMessage = channel.ErrorMessage,
                ChannelRetryCount = channel.RetryCount,
                Media = media
                    .Select(item =>
                    {
                        deliveryMap.TryGetValue(
                            (channel.Id, item.Id),
                            out var delivery);

                        return new PostMediaDeliveryItemDto
                        {
                            MediaId = item.Id,
                            FileName = item.FileName,
                            MediaType = item.MediaType,
                            FileSize = item.FileSize,
                            Status = delivery?.Status ?? DeliveryStatus.Pending,
                            SentAt = delivery?.SentAt,
                            ExternalMessageId = delivery?.ExternalMessageId,
                            ErrorMessage = delivery?.ErrorMessage,
                            RetryCount = delivery?.RetryCount ?? 0
                        };
                    })
                    .ToList()
            })
            .ToList();

        var allMediaReports = channelReports
            .SelectMany(x => x.Media)
            .ToList();

        return ResultDto<PostMediaDeliveryReportDto>.Ok(
            new PostMediaDeliveryReportDto
            {
                PostId = post.Id,
                PostTitle = post.Title,
                PostStatus = post.Status,
                TotalMedia = media.Count,
                TotalChannels = channels.Count,
                TotalDeliveries = allMediaReports.Count,
                SentCount = allMediaReports.Count(x => x.Status == DeliveryStatus.Sent),
                PendingCount = allMediaReports.Count(x => x.Status == DeliveryStatus.Pending),
                ProcessingCount = allMediaReports.Count(x => x.Status == DeliveryStatus.Processing),
                FailedCount = allMediaReports.Count(x => x.Status == DeliveryStatus.Failed),
                Channels = channelReports
            });
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

    public async Task<ResultDto<PostMediaDto>> UploadMediaAsync(
        long postId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<PostMediaDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        if (file is null || file.Length == 0)
            return ResultDto<PostMediaDto>.Fail("فایل ارسال نشده است.", 400);

        const long maxBytes = 50L * 1024 * 1024;
        if (file.Length > maxBytes)
            return ResultDto<PostMediaDto>.Fail("حداکثر حجم فایل 50 مگابایت است.", 400);

        const int maxMediaCount = 10;
        const long maxTotalBytes = 200L * 1024 * 1024;

        var currentMedia = await _database.PostMedia
            .Where(x => x.PostId == postId && !x.IsRemoved)
            .Select(x => new { x.FileSize })
            .ToListAsync(cancellationToken);

        if (currentMedia.Count >= maxMediaCount)
            return ResultDto<PostMediaDto>.Fail(
                "هر پست حداکثر می‌تواند 10 فایل رسانه‌ای داشته باشد.", 400);

        if (currentMedia.Sum(x => x.FileSize) + file.Length > maxTotalBytes)
            return ResultDto<PostMediaDto>.Fail(
                "مجموع حجم فایل‌های هر پست نمی‌تواند بیشتر از 200 مگابایت باشد.", 400);

        var mediaType = ResolveMediaType(file.ContentType);
        if (!mediaType.HasValue)
            return ResultDto<PostMediaDto>.Fail(
                "نوع فایل پشتیبانی نمی‌شود. فقط تصویر، ویدئو، صدا و فایل مجاز است.", 400);

        var post = await _database.Posts
            .SingleOrDefaultAsync(
                x => x.Id == postId &&
                     x.UserId == userId.Value &&
                     !x.IsRemoved,
                cancellationToken);

        if (post is null)
            return ResultDto<PostMediaDto>.Fail("پست موردنظر پیدا نشد.", 404);

        if (post.Status is PostStatus.Processing or PostStatus.Completed)
            return ResultDto<PostMediaDto>.Fail(
                "پست در این وضعیت قابل ویرایش نیست.", 409);

        await using var stream = file.OpenReadStream();

        var stored = await _mediaStorage.SaveAsync(
            stream,
            Path.GetFileName(file.FileName),
            file.ContentType,
            cancellationToken);

        var media = new PostMedia
        {
            PostId = post.Id,
            MediaType = mediaType.Value,
            FileName = stored.FileName,
            FileUrl = stored.FileUrl,
            FileSize = stored.FileSize
        };

        _database.PostMedia.Add(media);
        await _database.SaveChangesAsync(cancellationToken);

        return ResultDto<PostMediaDto>.Ok(new PostMediaDto
        {
            Id = media.Id,
            MediaType = media.MediaType,
            FileName = media.FileName,
            FileUrl = media.FileUrl,
            FileSize = media.FileSize
        });
    }

    private static MediaType? ResolveMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return MediaType.Image;

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            return MediaType.Video;

        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            return MediaType.Audio;

        if (contentType is "application/pdf" ||
            contentType is "application/msword" ||
            contentType is "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
            contentType is "application/vnd.ms-excel" ||
            contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
            contentType is "application/zip" ||
            contentType is "application/octet-stream")
            return MediaType.Document;

        return null;
    }

    public async Task<ResultDto> DeleteMediaAsync(
        long postId,
        long mediaId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto.Fail("کاربر جاری شناسایی نشد.", 401);

        var media = await _database.PostMedia
            .Include(x => x.Post)
            .SingleOrDefaultAsync(
                x => x.Id == mediaId &&
                     x.PostId == postId &&
                     x.Post.UserId == userId.Value &&
                     !x.IsRemoved &&
                     !x.Post.IsRemoved,
                cancellationToken);

        if (media is null)
            return ResultDto.Fail("فایل موردنظر پیدا نشد.", 404);

        if (media.Post.Status is PostStatus.Processing or PostStatus.Completed)
            return ResultDto.Fail(
                "پست در این وضعیت قابل ویرایش نیست.", 409);

        media.IsRemoved = true;
        media.IsActive = false;

        await _database.SaveChangesAsync(cancellationToken);

        await _mediaStorage.DeleteAsync(media.FileUrl, cancellationToken);

        return ResultDto.Ok("فایل با موفقیت حذف شد.");
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
        var schedule = await _database.Posts
            .Where(x => x.Id == id && !x.IsRemoved)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.Status,
                Schedule = x.Schedule == null
                    ? null
                    : new
                    {
                        x.Schedule.ScheduleType,
                        x.Schedule.NextRunAt,
                        x.Schedule.IsCompleted
                    }
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (schedule is null)
            return ResultDto.Fail("پست زمان‌بندی‌شده پیدا نشد.", 404);

        if (schedule.Status != PostStatus.Scheduled || schedule.Schedule is null)
            return ResultDto.Ok("پست در وضعیت قابل اجرای زمان‌بندی نیست.");

        var isRecurring =
            schedule.Schedule.ScheduleType is
                ScheduleType.Daily or
                ScheduleType.Weekly or
                ScheduleType.Monthly or
                ScheduleType.Cron;

        if (!isRecurring &&
            schedule.Schedule.NextRunAt.HasValue &&
            schedule.Schedule.NextRunAt.Value > DateTime.UtcNow)
        {
            return ResultDto.Ok("زمان اجرای پست هنوز نرسیده است.");
        }

        if (isRecurring)
        {
            var channels = await _database.PostChannels
                .Where(x => x.PostId == id && !x.IsRemoved)
                .ToListAsync(cancellationToken);

            foreach (var channel in channels)
            {
                channel.Status = DeliveryStatus.Pending;
                channel.SentAt = null;
                channel.ExternalMessageId = null;
                channel.ErrorMessage = null;
                channel.RetryCount = 0;
            }

            await _database.SaveChangesAsync(cancellationToken);
        }

        var result = await PublishInternalAsync(
            schedule.Id,
            schedule.UserId,
            cancellationToken);

        var postSchedule = await _database.PostSchedules
            .SingleOrDefaultAsync(
                x => x.PostId == id && !x.IsRemoved,
                cancellationToken);

        if (postSchedule is null)
            return result;

        postSchedule.LastRunAt = DateTime.UtcNow;

        if (isRecurring)
        {
            postSchedule.IsCompleted = false;
            postSchedule.NextRunAt = null;

            var post = await _database.Posts
                .SingleAsync(x => x.Id == id, cancellationToken);

            post.Status = PostStatus.Scheduled;
        }
        else
        {
            postSchedule.IsCompleted = true;
            postSchedule.NextRunAt = null;
        }

        await _database.SaveChangesAsync(cancellationToken);

        return result;
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
            .Include(x => x.Media)
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

                var result = await SendPostAsync(
                    sender,
                    credentials,
                    target,
                    post,
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

            if (target.Status == DeliveryStatus.Failed && target.RetryCount < 5)
            {
                await _retryScheduler.ScheduleAsync(
                    target.Id,
                    target.RetryCount,
                    cancellationToken);
            }
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

            delivery.Status = DeliveryStatus.Sent;
            delivery.SentAt = DateTime.UtcNow;
            delivery.ExternalMessageId = lastResult.ExternalMessageId;
            delivery.ErrorMessage = null;
            captionUsed = true;

            await _database.SaveChangesAsync(cancellationToken);
        }

        return lastResult ?? new SenderResult(
            true,
            ErrorMessage: null);
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
