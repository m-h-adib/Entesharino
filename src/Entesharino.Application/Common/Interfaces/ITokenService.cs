using Entesharino.Domain.Entities;

namespace Entesharino.Application.Common.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(User user);
}
