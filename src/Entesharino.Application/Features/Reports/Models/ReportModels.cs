using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Reports.Models;

public sealed class DashboardRequest
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
