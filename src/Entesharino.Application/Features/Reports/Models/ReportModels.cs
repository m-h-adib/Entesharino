using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Reports.Models;

public class DashboardRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class DashboardDto
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public DashboardChannelStats Channels { get; init; } = new();
    public DashboardPostStats Posts { get; init; } = new();
    public DashboardDeliveryStats Deliveries { get; init; } = new();
    public DashboardMediaStats Media { get; init; } = new();
}

public sealed class DashboardChannelStats
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Connected { get; init; }
    public int Disconnected { get; init; }
}

public sealed class DashboardPostStats
{
    public int Total { get; init; }
    public int Draft { get; init; }
    public int Scheduled { get; init; }
    public int Processing { get; init; }
    public int Completed { get; init; }
    public int PartiallyCompleted { get; init; }
    public int Failed { get; init; }
    public int Cancelled { get; init; }
}

public sealed class DashboardDeliveryStats
{
    public int Total { get; init; }
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Sent { get; init; }
    public int Failed { get; init; }
    public int Cancelled { get; init; }
}

public sealed class DashboardMediaStats
{
    public int Total { get; init; }
    public long TotalSize { get; init; }
    public int Sent { get; init; }
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Failed { get; init; }
    public int Cancelled { get; init; }
}


public sealed class DeliveryReportRequest : DashboardRequest
{
    public long? ChannelId { get; set; }
    public PlatformType? Platform { get; set; }
    public DeliveryStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class DeliveryReportItemDto
{
    public long PostId { get; init; }
    public string PostTitle { get; init; } = string.Empty;
    public long ChannelId { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public PlatformType Platform { get; init; }
    public DeliveryStatus Status { get; init; }
    public int RetryCount { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public string? ExternalMessageId { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class DeliveryReportDto
{
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public IReadOnlyList<DeliveryReportItemDto> Items { get; init; } = [];
}

public sealed class ChannelDeliverySummaryDto
{
    public long ChannelId { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public PlatformType Platform { get; init; }
    public int Total { get; init; }
    public int Sent { get; init; }
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Failed { get; init; }
    public int Cancelled { get; init; }
}

public sealed class DeliverySummaryDto
{
    public int Total { get; init; }
    public int Sent { get; init; }
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Failed { get; init; }
    public int Cancelled { get; init; }
    public IReadOnlyList<ChannelDeliverySummaryDto> Channels { get; init; } = [];
    public IReadOnlyList<DeliveryReportItemDto> RecentFailures { get; init; } = [];
}
