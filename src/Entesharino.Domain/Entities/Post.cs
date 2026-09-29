using Entesharino.Domain.Common;
using Entesharino.Domain.Enums;

namespace Entesharino.Domain.Entities;

public class Post : BaseEntity
{
    public long UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Draft;

    public User User { get; set; } = null!;
    public ICollection<PostMedia> Media { get; set; } = new List<PostMedia>();
    public ICollection<PostChannel> Channels { get; set; } = new List<PostChannel>();
    public PostSchedule? Schedule { get; set; }
}