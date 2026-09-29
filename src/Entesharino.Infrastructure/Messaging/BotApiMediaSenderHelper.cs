using System.Net.Http.Headers;
using System.Text.Json;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Messaging;

internal static class BotApiMediaSenderHelper
{
    public static async Task<SenderResult> SendAsync(
        HttpClient httpClient,
        string endpoint,
        string chatId,
        MediaMessage media,
        string fileFieldName,
        CancellationToken cancellationToken)
    {
        if (media.Content is null)
            throw new ArgumentNullException(nameof(media.Content));

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(chatId), "chat_id");

        if (!string.IsNullOrWhiteSpace(media.Caption))
            form.Add(new StringContent(media.Caption), "caption");

        var streamContent = new StreamContent(media.Content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(media.ContentType)
                ? "application/octet-stream"
                : media.ContentType);

        form.Add(streamContent, fileFieldName, media.FileName);

        using var response = await httpClient.PostAsync(
            endpoint,
            form,
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
                ErrorMessage: $"Bot API خطا برگرداند: {(int)response.StatusCode}.");

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
            {
                var description = root.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : "خطای نامشخص در Bot API.";

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
                ErrorMessage: "پاسخ دریافتی از Bot API معتبر نیست.");
        }
    }
}
