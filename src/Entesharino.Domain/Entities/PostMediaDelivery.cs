using Entesharino.Domain.Common;
using Entesharino.Domain.Enums;

namespace Entesharino.Domain.Entities;

public class PostMediaDelivery : BaseEntity
{
    public long PostChannelId { get; set; }
    public long PostMediaId { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTime? SentAt { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public PostChannel PostChannel { get; set; } = null!;
    public PostMedia PostMedia { get; set; } = null!;
}
