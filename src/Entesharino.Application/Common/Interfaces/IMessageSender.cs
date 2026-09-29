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

    Task<SenderResult> SendMediaAsync(
        ChannelCredentials credentials,
        MediaMessage media,
        CancellationToken cancellationToken = default);
}

public sealed record MediaMessage(
    MediaType MediaType,
    Stream Content,
    string FileName,
    string ContentType,
    string? Caption = null);

public sealed record ChannelCredentials(
    string AccessToken,
    string Identifier,
    string? RefreshToken = null);

public sealed record SenderResult(
    bool Success,
    string? ExternalMessageId = null,
    string? ErrorMessage = null);
