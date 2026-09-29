using Entesharino.Domain.Common;
using Entesharino.Domain.Enums;

namespace Entesharino.Domain.Entities;

public class PostChannel : BaseEntity
{
    public long PostId { get; set; }
    public long ChannelId { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public Post Post { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
    public ICollection<DeliveryAttempt> DeliveryAttempts { get; set; } = new List<DeliveryAttempt>();
}