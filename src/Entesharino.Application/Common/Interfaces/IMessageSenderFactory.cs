using Entesharino.Domain.Enums;

namespace Entesharino.Application.Common.Interfaces;

public interface IMessageSenderFactory
{
    IMessageSender Get(PlatformType platform);
}
