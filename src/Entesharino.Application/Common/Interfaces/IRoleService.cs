using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Roles.Models;

namespace Entesharino.Application.Common.Interfaces;

public interface IRoleService
{
    Task<ResultOfList<RoleListItemDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<RoleDetailsDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ResultOfList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<RoleDetailsDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<RoleDetailsDto>> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken cancellationToken = default);
}
