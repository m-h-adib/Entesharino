using Entesharino.Domain.Entities;

namespace Entesharino.Application.Common.Interfaces;

public interface ITokenService
{
    Task<string> CreateAccessTokenAsync(
        User user,
        CancellationToken cancellationToken = default);
}
