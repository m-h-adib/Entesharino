using System.Text;
using System.Text.Json;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Messaging;

public abstract class UnsupportedMessageSender : IMessageSender
{
    public abstract PlatformType Platform { get; }

    public Task<SenderResult> TestConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SenderResult(false,
            ErrorMessage: $"اتصال به پلتفرم {Platform} هنوز پیاده‌سازی نشده است."));

    public Task<SenderResult> SendTextAsync(
        ChannelCredentials credentials,
        string text,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SenderResult(false,
            ErrorMessage: $"ارسال پیام به پلتفرم {Platform} هنوز پیاده‌سازی نشده است."));
}

public sealed class RubikaMessageSender : IMessageSender
{
    private readonly HttpClient _httpClient;

    public RubikaMessageSender(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public PlatformType Platform => PlatformType.Rubika;

    public async Task<SenderResult> TestConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            $"{Uri.EscapeDataString(credentials.AccessToken)}/getMe",
            content: null,
            cancellationToken);

        return await ParseResultAsync(response, cancellationToken);
    }

    public async Task<SenderResult> SendTextAsync(
        ChannelCredentials credentials,
        string text,
        CancellationToken cancellationToken = default)
    {
        var payload = new { chat_id = credentials.Identifier, text };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(
            $"{Uri.EscapeDataString(credentials.AccessToken)}/sendMessage",
            content,
            cancellationToken);

        return await ParseResultAsync(response, cancellationToken);
    }

    private static async Task<SenderResult> ParseResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new SenderResult(false,
                ErrorMessage: $"Rubika Bot API خطا برگرداند: {(int)response.StatusCode}.");

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("ok", out var ok) &&
                ok.ValueKind is JsonValueKind.False)
                return new SenderResult(false, ErrorMessage: GetError(root));

            if (root.TryGetProperty("status", out var status) &&
                status.ValueKind == JsonValueKind.String &&
                !string.Equals(status.GetString(), "OK", StringComparison.OrdinalIgnoreCase))
                return new SenderResult(false, ErrorMessage: GetError(root));

            string? messageId = null;

            if (root.TryGetProperty("message_id", out var directId))
                messageId = directId.ToString();
            else if (root.TryGetProperty("data", out var data) &&
                     data.ValueKind == JsonValueKind.Object &&
                     data.TryGetProperty("message_id", out var nestedId))
                messageId = nestedId.ToString();

            return new SenderResult(true, ExternalMessageId: messageId);
        }
        catch (JsonException)
        {
            return new SenderResult(false,
                ErrorMessage: "پاسخ دریافتی از Rubika Bot API معتبر نیست.");
        }
    }

    private static string GetError(JsonElement root)
    {
        foreach (var name in new[] { "error", "message", "description" })
        {
            if (root.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
                return value.GetString()!;
        }

        return "خطای نامشخص از Rubika Bot API.";
    }
}
