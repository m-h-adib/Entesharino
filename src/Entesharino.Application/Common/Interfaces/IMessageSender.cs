using Entesharino.Domain.Enums;

namespace Entesharino.Application.Common.Interfaces;

public interface IMessageSender
{
    PlatformType Platform { get; }

    Task<SenderResult> TestConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default);

    Task<SenderResult> SendTextAsync(
        ChannelCredentials credentials,
        string text,
        CancellationToken cancellationToken = default);
}

public sealed record ChannelCredentials(
    string AccessToken,
    string Identifier,
    string? RefreshToken = null);

public sealed record SenderResult(
    bool Success,
    string? ExternalMessageId = null,
    string? ErrorMessage = null);
