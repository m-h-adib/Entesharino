namespace Entesharino.Application.Features.Channels.Models;

public sealed class SetChannelUsersRequest
{
    public List<long> UserIds { get; set; } = [];
}

public sealed class ChannelUserAccessDto
{
    public long UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public bool IsOwner { get; init; }
    public bool HasAccess { get; init; }
}
