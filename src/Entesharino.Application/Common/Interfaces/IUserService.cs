using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Users.Models;

namespace Entesharino.Application.Common.Interfaces;

public interface IUserService
{
    Task<ResultOfList<UserListItemDto>> GetListAsync(UserListRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<UserDetailsDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ResultDto<UserDetailsDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<UserDetailsDto>> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task<ResultDto> AssignRoleAsync(long id, long roleId, CancellationToken cancellationToken = default);
    Task<ResultDto> ResetPasswordAsync(long id, string password, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(long id, CancellationToken cancellationToken = default);
}