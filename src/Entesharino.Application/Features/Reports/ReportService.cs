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
            return ResultDto<DashboardDto>.Fail("بازه زمانی نامعتبر است.", 400);
        }

        var from = request.From;
        var toExclusive = request.To?.Date.AddDays(1);

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

        var media = _database.PostMedia
            .AsNoTracking()
            .Where(x => !x.IsRemoved && postIds.Contains(x.PostId));

        var deliveries = _database.PostMediaDeliveries
            .AsNoTracking()
            .Where(x => !x.IsRemoved && postIds.Contains(x.PostChannel.PostId));

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

        var postCounts = postStatusCounts.ToDictionary(x => x.Status, x => x.Count);

        var postStats = new DashboardPostStats
        {
            Total = postCounts.Values.Sum(),
            Draft = Get(postCounts, PostStatus.Draft),
            Scheduled = Get(postCounts, PostStatus.Scheduled),
            Processing = Get(postCounts, PostStatus.Processing),
            Completed = Get(postCounts, PostStatus.Completed),
            PartiallyCompleted = Get(postCounts, PostStatus.PartiallyCompleted),
            Failed = Get(postCounts, PostStatus.Failed),
            Cancelled = Get(postCounts, PostStatus.Cancelled)
        };

        var deliveryStatusCounts = await postChannels
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var deliveryCounts = deliveryStatusCounts.ToDictionary(x => x.Status, x => x.Count);

        var deliveryStats = new DashboardDeliveryStats
        {
            Total = deliveryCounts.Values.Sum(),
            Pending = Get(deliveryCounts, DeliveryStatus.Pending),
            Processing = Get(deliveryCounts, DeliveryStatus.Processing),
            Sent = Get(deliveryCounts, DeliveryStatus.Sent),
            Failed = Get(deliveryCounts, DeliveryStatus.Failed),
            Cancelled = Get(deliveryCounts, DeliveryStatus.Cancelled)
        };

        var mediaDeliveryStatusCounts = await deliveries
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var mediaDeliveryCounts = mediaDeliveryStatusCounts
            .ToDictionary(x => x.Status, x => x.Count);

        var mediaStats = new DashboardMediaStats
        {
            Total = await media.CountAsync(cancellationToken),
            TotalSize = await media.SumAsync(x => (long?)x.FileSize, cancellationToken) ?? 0,
            Sent = Get(mediaDeliveryCounts, DeliveryStatus.Sent),
            Pending = Get(mediaDeliveryCounts, DeliveryStatus.Pending),
            Processing = Get(mediaDeliveryCounts, DeliveryStatus.Processing),
            Failed = Get(mediaDeliveryCounts, DeliveryStatus.Failed),
            Cancelled = Get(mediaDeliveryCounts, DeliveryStatus.Cancelled)
        };

        return ResultDto<DashboardDto>.Ok(new DashboardDto
        {
            From = from,
            To = request.To,
            Channels = channelStats,
            Posts = postStats,
            Deliveries = deliveryStats,
            Media = mediaStats
        });
    }

    private static int Get<T>(
        IReadOnlyDictionary<T, int> counts,
        T key)
        where T : struct, Enum =>
        counts.TryGetValue(key, out var count) ? count : 0;
}
