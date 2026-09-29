using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Reports.Models;
using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Reports;

public sealed class ReportService : IReportService
{
    private readonly IDatabaseContext _database;
    private readonly ICurrentUserService _currentUser;

    public ReportService(
        IDatabaseContext database,
        ICurrentUserService currentUser)
    {
        _database = database;
        _currentUser = currentUser;
    }

    public async Task<ResultDto<DashboardDto>> GetDashboardAsync(
        DashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<DashboardDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        if (request.From.HasValue && request.To.HasValue &&
            request.From.Value > request.To.Value)
        {
            return ResultDto<DashboardDto>.Fail(
                "بازه زمانی نامعتبر است.",
                400);
        }

        var from = request.From;
        var toExclusive = request.To?.AddDays(1);

        var channels = _database.Channels
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsRemoved);

        var posts = _database.Posts
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsRemoved);

        if (from.HasValue)
            posts = posts.Where(x => x.CreatedAt >= from.Value);

        if (toExclusive.HasValue)
            posts = posts.Where(x => x.CreatedAt < toExclusive.Value);

        var postIds = posts.Select(x => x.Id);

        var postChannels = _database.PostChannels
            .AsNoTracking()
            .Where(x => !x.IsRemoved && postIds.Contains(x.PostId));

        var deliveries = _database.PostMediaDeliveries
            .AsNoTracking()
            .Where(x => !x.IsRemoved && postIds.Contains(x.PostChannel.PostId));

        var media = _database.PostMedia
            .AsNoTracking()
            .Where(x => !x.IsRemoved && postIds.Contains(x.PostId));

        var channelStats = new DashboardChannelStats
        {
            Total = await channels.CountAsync(cancellationToken),
            Active = await channels.CountAsync(x => x.IsActive, cancellationToken),
            Connected = await channels.CountAsync(x => x.IsConnected, cancellationToken),
            Disconnected = await channels.CountAsync(x => !x.IsConnected, cancellationToken)
        };

        var postStatusCounts = await posts
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var postStats = new DashboardPostStats
        {
            Total = postStatusCounts.Sum(x => x.Count),
            Draft = Count(postStatusCounts, PostStatus.Draft),
            Scheduled = Count(postStatusCounts, PostStatus.Scheduled),
            Processing = Count(postStatusCounts, PostStatus.Processing),
            Completed = Count(postStatusCounts, PostStatus.Completed),
            PartiallyCompleted = Count(postStatusCounts, PostStatus.PartiallyCompleted),
            Failed = Count(postStatusCounts, PostStatus.Failed),
            Cancelled = Count(postStatusCounts, PostStatus.Cancelled)
        };

        var deliveryStatusCounts = await postChannels
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var deliveryStats = new DashboardDeliveryStats
        {
            Total = deliveryStatusCounts.Sum(x => x.Count),
            Pending = Count(deliveryStatusCounts, DeliveryStatus.Pending),
            Processing = Count(deliveryStatusCounts, DeliveryStatus.Processing),
            Sent = Count(deliveryStatusCounts, DeliveryStatus.Sent),
            Failed = Count(deliveryStatusCounts, DeliveryStatus.Failed),
            Cancelled = Count(deliveryStatusCounts, DeliveryStatus.Cancelled)
        };

        var mediaStats = await media
            .GroupJoin(
                deliveries,
                m => m.Id,
                d => d.PostMediaId,
                (m, ds) => new { m.FileSize, Deliveries = ds })
            .SelectMany(
                x => x.Deliveries.DefaultIfEmpty(),
                (x, d) => new
                {
                    x.FileSize,
                    Status = d == null ? (DeliveryStatus?)null : d.Status
                })
            .ToListAsync(cancellationToken);

        var mediaStatusCounts = mediaStats
            .Where(x => x.Status.HasValue)
            .GroupBy(x => x.Status!.Value)
            .ToDictionary(x => x.Key, x => x.Count());

        var mediaStatsDto = new DashboardMediaStats
        {
            Total = await media.CountAsync(cancellationToken),
            TotalSize = await media.SumAsync(x => (long?)x.FileSize, cancellationToken) ?? 0,
            Sent = Get(mediaStatusCounts, DeliveryStatus.Sent),
            Pending = Get(mediaStatusCounts, DeliveryStatus.Pending),
            Processing = Get(mediaStatusCounts, DeliveryStatus.Processing),
            Failed = Get(mediaStatusCounts, DeliveryStatus.Failed),
            Cancelled = Get(mediaStatusCounts, DeliveryStatus.Cancelled)
        };

        return ResultDto<DashboardDto>.Ok(new DashboardDto
        {
            From = from,
            To = request.To,
            Channels = channelStats,
            Posts = postStats,
            Deliveries = deliveryStats,
            Media = mediaStatsDto
        });
    }

    private static int Count<T>(
        IReadOnlyCollection<T> items,
        PostStatus status)
        where T : class
    {
        return items.Count(x => (PostStatus)(x.GetType().GetProperty("Status")!.GetValue(x)!) == status);
    }

    private static int Count<T>(
        IReadOnlyCollection<T> items,
        DeliveryStatus status)
        where T : class
    {
        return items.Count(x => (DeliveryStatus)(x.GetType().GetProperty("Status")!.GetValue(x)!) == status);
    }

    private static int Get(
        IReadOnlyDictionary<DeliveryStatus, int> counts,
        DeliveryStatus status) =>
        counts.TryGetValue(status, out var count) ? count : 0;
}
