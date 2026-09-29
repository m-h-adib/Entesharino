namespace Entesharino.Application.Features.Roles.Models;

public sealed class RoleListItemDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int PermissionCount { get; init; }
}

public sealed class RoleDetailsDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<PermissionDto> Permissions { get; init; } = [];
}

public sealed class PermissionDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

public sealed class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class UpdateRoleRequest
{
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class SetRolePermissionsRequest
{
    public IReadOnlyList<long> PermissionIds { get; set; } = [];
}
