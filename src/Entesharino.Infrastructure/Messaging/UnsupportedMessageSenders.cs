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


    public async Task<SenderResult> SendMediaAsync(
        ChannelCredentials credentials,
        MediaMessage media,
        CancellationToken cancellationToken = default)
    {
        var fileType = media.MediaType switch
        {
            MediaType.Image => "Image",
            MediaType.Video => "Video",
            MediaType.Audio => "Music",
            MediaType.Document => "File",
            _ => throw new InvalidOperationException(
                $"نوع رسانه '{media.MediaType}' پشتیبانی نمی‌شود.")
        };

        var requestPayload = JsonSerializer.Serialize(new { type = fileType });

        using var requestContent = new StringContent(
            requestPayload,
            Encoding.UTF8,
            "application/json");

        using var requestResponse = await _httpClient.PostAsync(
            $"{Uri.EscapeDataString(credentials.AccessToken)}/requestSendFile",
            requestContent,
            cancellationToken);

        var requestBody = await requestResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!requestResponse.IsSuccessStatusCode)
            return new SenderResult(
                false,
                ErrorMessage: $"Rubika Bot API خطا در آماده‌سازی آپلود برگرداند: {(int)requestResponse.StatusCode}.");

        string? uploadUrl;
        try
        {
            using var document = JsonDocument.Parse(requestBody);
            var root = document.RootElement;

            if (root.TryGetProperty("ok", out var ok) &&
                ok.ValueKind == JsonValueKind.False)
                return new SenderResult(false, ErrorMessage: GetError(root));

            uploadUrl = root.TryGetProperty("upload_url", out var directUrl)
                ? directUrl.GetString()
                : root.TryGetProperty("data", out var data) &&
                  data.ValueKind == JsonValueKind.Object &&
                  data.TryGetProperty("upload_url", out var nestedUrl)
                    ? nestedUrl.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return new SenderResult(
                false,
                ErrorMessage: "پاسخ آماده‌سازی آپلود Rubika معتبر نیست.");
        }

        if (string.IsNullOrWhiteSpace(uploadUrl))
            return new SenderResult(
                false,
                ErrorMessage: "Rubika Bot API آدرس آپلود فایل را برنگرداند.");

        using var uploadForm = new MultipartFormDataContent();

        var streamContent = new StreamContent(media.Content);
        streamContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(media.ContentType)
                    ? "application/octet-stream"
                    : media.ContentType);

        uploadForm.Add(streamContent, "file", media.FileName);

        using var uploadResponse = await _httpClient.PostAsync(
            uploadUrl,
            uploadForm,
            cancellationToken);

        var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!uploadResponse.IsSuccessStatusCode)
            return new SenderResult(
                false,
                ErrorMessage: $"Rubika Bot API خطا در آپلود فایل برگرداند: {(int)uploadResponse.StatusCode}.");

        string? fileId;
        try
        {
            using var document = JsonDocument.Parse(uploadBody);
            var root = document.RootElement;

            if (root.TryGetProperty("ok", out var ok) &&
                ok.ValueKind == JsonValueKind.False)
                return new SenderResult(false, ErrorMessage: GetError(root));

            fileId = root.TryGetProperty("file_id", out var directId)
                ? directId.GetString()
                : root.TryGetProperty("data", out var data) &&
                  data.ValueKind == JsonValueKind.Object &&
                  data.TryGetProperty("file_id", out var nestedId)
                    ? nestedId.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return new SenderResult(
                false,
                ErrorMessage: "پاسخ آپلود فایل Rubika معتبر نیست.");
        }

        if (string.IsNullOrWhiteSpace(fileId))
            return new SenderResult(
                false,
                ErrorMessage: "Rubika Bot API شناسه فایل را برنگرداند.");

        var sendPayload = new
        {
            chat_id = credentials.Identifier,
            file_id = fileId,
            text = media.Caption
        };

        using var sendContent = new StringContent(
            JsonSerializer.Serialize(sendPayload),
            Encoding.UTF8,
            "application/json");

        using var sendResponse = await _httpClient.PostAsync(
            $"{Uri.EscapeDataString(credentials.AccessToken)}/sendFile",
            sendContent,
            cancellationToken);

        return await ParseResultAsync(sendResponse, cancellationToken);
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
