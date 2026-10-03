using Entesharino.Domain.Common;
using Entesharino.Domain.Enums;

namespace Entesharino.Domain.Entities;

public class Channel : BaseEntity
{
    public long UserId { get; set; }
    public PlatformType Platform { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? LastConnectionCheckAt { get; set; }
    public bool IsConnected { get; set; }

    public User User { get; set; } = null!;
    public ChannelConnection? Connection { get; set; }
    public ICollection<PostChannel> PostChannels { get; set; } = new List<PostChannel>();
    public ICollection<ChannelUserAccess> UserAccesses { get; set; } = new List<ChannelUserAccess>();
}