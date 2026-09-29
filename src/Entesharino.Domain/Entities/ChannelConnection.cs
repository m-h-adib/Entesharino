using Entesharino.Domain.Common;

namespace Entesharino.Domain.Entities;

public class ChannelConnection : BaseEntity
{
    public long ChannelId { get; set; }
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string? EncryptedRefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? LastValidatedAt { get; set; }

    public Channel Channel { get; set; } = null!;
}