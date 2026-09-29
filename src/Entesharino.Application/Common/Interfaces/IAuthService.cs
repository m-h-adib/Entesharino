using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Auth.Models;

namespace Entesharino.Application.Common.Interfaces;

public interface IAuthService
{
    Task<ResultDto<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultDto<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
