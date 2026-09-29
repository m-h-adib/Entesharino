using System.Text.Json;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Messaging;

public sealed class EitaaMessageSender : IMessageSender
{
    private readonly HttpClient _httpClient;

    public EitaaMessageSender(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public PlatformType Platform => PlatformType.Eitaa;

    public async Task<SenderResult> TestConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        using var content = new FormUrlEncodedContent([]);

        var response = await _httpClient.PostAsync(
            $"{Uri.EscapeDataString(credentials.AccessToken)}/getMe",
            content,
            cancellationToken);

        return await ParseResultAsync(response, cancellationToken);
    }

    public async Task<SenderResult> SendTextAsync(
        ChannelCredentials credentials,
        string text,
        CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>
        {
            ["chat_id"] = credentials.Identifier,
            ["text"] = text
        };

        using var content = new FormUrlEncodedContent(values);

        var response = await _httpClient.PostAsync(
            $"{Uri.EscapeDataString(credentials.AccessToken)}/sendMessage",
            content,
            cancellationToken);

        return await ParseResultAsync(response, cancellationToken);
    }


    public Task<SenderResult> SendMediaAsync(
        ChannelCredentials credentials,
        MediaMessage media,
        CancellationToken cancellationToken = default)
    {
        var (method, fieldName) = media.MediaType switch
        {
            MediaType.Image => ("sendPhoto", "photo"),
            MediaType.Video => ("sendVideo", "video"),
            MediaType.Audio => ("sendAudio", "audio"),
            MediaType.Document => ("sendDocument", "document"),
            _ => throw new InvalidOperationException(
                $"نوع رسانه '${media.MediaType}' پشتیبانی نمی‌شود.")
        };

        return BotApiMediaSenderHelper.SendAsync(
            _httpClient,
            $"{Uri.EscapeDataString(credentials.AccessToken)}/{method}",
            credentials.Identifier,
            media,
            fieldName,
            cancellationToken);
    }

    private static async Task<SenderResult> ParseResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new SenderResult(
                false,
                ErrorMessage: $"EitaaYar API خطا برگرداند: {(int)response.StatusCode}.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
            {
                var description = root.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : "خطای نامشخص در EitaaYar.";

                return new SenderResult(
                    false,
                    ErrorMessage: description);
            }

            string? messageId = null;

            if (root.TryGetProperty("result", out var result) &&
                result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("message_id", out var id))
            {
                messageId = id.ToString();
            }

            return new SenderResult(
                true,
                ExternalMessageId: messageId);
        }
        catch (JsonException)
        {
            return new SenderResult(
                false,
                ErrorMessage: "پاسخ دریافتی از EitaaYar معتبر نیست.");
        }
    }
}
