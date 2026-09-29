using Entesharino.Domain.Common;
using Entesharino.Domain.Enums;

namespace Entesharino.Domain.Entities;

public class PostMedia : BaseEntity
{
    public long PostId { get; set; }
    public MediaType MediaType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public Post Post { get; set; } = null!;
}