using Entesharino.Domain.Common;
using Entesharino.Domain.Enums;

namespace Entesharino.Domain.Entities;

public class PostSchedule : BaseEntity
{
    public long PostId { get; set; }
    public ScheduleType ScheduleType { get; set; } = ScheduleType.OneTime;
    public DateTime? ScheduledAt { get; set; }
    public string? CronExpression { get; set; }
    public string? TimeZone { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public bool IsCompleted { get; set; }

    public Post Post { get; set; } = null!;
}