using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Messaging;

public abstract class UnsupportedMessageSender : IMessageSender
{
    public abstract PlatformType Platform { get; }

    public Task<SenderResult> TestConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SenderResult(
            false,
            ErrorMessage: $"اتصال به پلتفرم {Platform} هنوز پیاده‌سازی نشده است."));

    public Task<SenderResult> SendTextAsync(
        ChannelCredentials credentials,
        string text,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SenderResult(
            false,
            ErrorMessage: $"ارسال پیام به پلتفرم {Platform} هنوز پیاده‌سازی نشده است."));
}

public sealed class EitaaMessageSender : UnsupportedMessageSender
{
    public override PlatformType Platform => PlatformType.Eitaa;
}

public sealed class BaleMessageSender : UnsupportedMessageSender
{
    public override PlatformType Platform => PlatformType.Bale;
}

public sealed class RubikaMessageSender : UnsupportedMessageSender
{
    public override PlatformType Platform => PlatformType.Rubika;
}
