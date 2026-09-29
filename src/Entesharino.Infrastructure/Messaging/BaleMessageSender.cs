using System.Net.Http.Json;
using System.Text.Json;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Messaging;

public sealed class BaleMessageSender : IMessageSender
{
    private readonly HttpClient _httpClient;

    public BaleMessageSender(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public PlatformType Platform => PlatformType.Bale;

    public async Task<SenderResult> TestConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"bot{credentials.AccessToken}/getMe",
            cancellationToken);

        return await ParseResultAsync(response, cancellationToken);
    }

    public async Task<SenderResult> SendTextAsync(
        ChannelCredentials credentials,
        string text,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            chat_id = credentials.Identifier,
            text
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"bot{credentials.AccessToken}/sendMessage",
            payload,
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
            $"bot{credentials.AccessToken}/{method}",
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
            return new SenderResult(
                false,
                ErrorMessage: $"Bale API خطا برگرداند: {(int)response.StatusCode}.");

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
            {
                var description = root.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : "خطای نامشخص در Bale.";

                return new SenderResult(false, ErrorMessage: description);
            }

            string? messageId = null;

            if (root.TryGetProperty("result", out var result) &&
                result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("message_id", out var id))
            {
                messageId = id.ToString();
            }

            return new SenderResult(true, messageId);
        }
        catch (JsonException)
        {
            return new SenderResult(
                false,
                ErrorMessage: "پاسخ دریافتی از Bale معتبر نیست.");
        }
    }
}
