using System.Net.Http.Json;
using System.Text.Json;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Messaging;

public sealed class TelegramMessageSender : IMessageSender
{
    private readonly HttpClient _httpClient;

    public TelegramMessageSender(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public PlatformType Platform => PlatformType.Telegram;

    public async Task<SenderResult> TestConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"bot{Uri.EscapeDataString(credentials.AccessToken)}/getMe",
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
            $"bot{Uri.EscapeDataString(credentials.AccessToken)}/sendMessage",
            payload,
            cancellationToken);

        return await ParseResultAsync(response, cancellationToken);
    }

    private static async Task<SenderResult> ParseResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new SenderResult(
                false,
                ErrorMessage: $"Telegram API خطا برگرداند: {(int)response.StatusCode}.");

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
            {
                var description = root.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : "خطای نامشخص در Telegram.";

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
                ErrorMessage: "پاسخ دریافتی از Telegram معتبر نیست.");
        }
    }
}
