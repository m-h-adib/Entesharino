using Entesharino.Domain.Common;

namespace Entesharino.Domain.Entities;

public class ChannelUserAccess : BaseEntity
{
    public long ChannelId { get; set; }
    public long UserId { get; set; }

    public Channel Channel { get; set; } = null!;
    public User User { get; set; } = null!;
}
