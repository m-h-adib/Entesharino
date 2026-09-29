using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Channels.Models;

public sealed class ChannelListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public PlatformType? Platform { get; set; }
    public string? Search { get; set; }
}

public class ChannelListItemDto
{
    public long Id { get; init; }
    public PlatformType Platform { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Identifier { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public bool IsConnected { get; init; }
    public DateTime? LastConnectionCheckAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ChannelDetailsDto : ChannelListItemDto
{
}

public sealed class CreateChannelRequest
{
    public PlatformType Platform { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}

public sealed class UpdateChannelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateChannelConnectionRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}
