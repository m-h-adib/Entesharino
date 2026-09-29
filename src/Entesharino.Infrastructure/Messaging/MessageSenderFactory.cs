using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Messaging;

public sealed class MessageSenderFactory : IMessageSenderFactory
{
    private readonly IReadOnlyDictionary<PlatformType, IMessageSender> _senders;

    public MessageSenderFactory(IEnumerable<IMessageSender> senders)
    {
        _senders = senders.ToDictionary(x => x.Platform);
    }

    public IMessageSender Get(PlatformType platform)
    {
        if (_senders.TryGetValue(platform, out var sender))
            return sender;

        throw new InvalidOperationException(
            $"No message sender registered for platform '{platform}'.");
    }
}
