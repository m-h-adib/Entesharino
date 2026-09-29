using Entesharino.Domain.Common;

namespace Entesharino.Domain.Entities;

public class DeliveryAttempt : BaseEntity
{
    public long PostChannelId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public PostChannel PostChannel { get; set; } = null!;
}