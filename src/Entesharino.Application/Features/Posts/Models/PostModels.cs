using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Posts.Models;

public sealed class PostListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public PostStatus? Status { get; set; }
    public string? Search { get; set; }
}

public sealed class PostListItemDto
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public PostStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ChannelCount { get; init; }
    public int SentChannelCount { get; init; }
    public DateTime? ScheduledAt { get; init; }
}

public sealed class PostChannelDto
{
    public long ChannelId { get; init; }
    public string ChannelName { get; init; } = string.Empty;
    public PlatformType Platform { get; init; }
    public DeliveryStatus Status { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public DateTime? SentAt { get; init; }
    public string? ExternalMessageId { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
}

public sealed class PostMediaDto
{
    public long Id { get; init; }
    public MediaType MediaType { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public long FileSize { get; init; }
}

public sealed class PostDetailsDto
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Content { get; init; }
    public PostStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<PostChannelDto> Channels { get; init; } = [];
    public IReadOnlyList<PostMediaDto> Media { get; init; } = [];
    public PostScheduleDto? Schedule { get; init; }
}

public sealed class PostScheduleDto
{
    public ScheduleType ScheduleType { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public string? CronExpression { get; init; }
    public string? TimeZone { get; init; }
    public DateTime? NextRunAt { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class CreatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public List<long> ChannelIds { get; set; } = [];
}

public sealed class UpdatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public List<long> ChannelIds { get; set; } = [];
}

public sealed class SchedulePostRequest
{
    public ScheduleType ScheduleType { get; set; } = ScheduleType.OneTime;
    public DateTime? ScheduledAt { get; set; }
    public string? CronExpression { get; set; }
    public string? TimeZone { get; set; }
}
