using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Roles.Models;
using Entesharino.Domain.Entities;

namespace Entesharino.Application.Features.Roles;

public sealed class RoleService : IRoleService
{
    private readonly IDatabaseContext _database;

    public RoleService(IDatabaseContext database) => _database = database;

    public async Task<ResultOfList<RoleListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _database.Roles.AsNoTracking()
            .Where(x => !x.IsRemoved)
            .OrderBy(x => x.Name)
            .Select(x => new RoleListItemDto
            {
                Id = x.Id, Name = x.Name, DisplayName = x.DisplayName,
                IsActive = x.IsActive, PermissionCount = x.RolePermissions.Count
            }).ToListAsync(cancellationToken);
        return ResultOfList<RoleListItemDto>.Ok(roles, roles.Count);
    }

    public async Task<ResultDto<RoleDetailsDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await _database.Roles.AsNoTracking()
            .Where(x => x.Id == id && !x.IsRemoved)
            .Select(x => new RoleDetailsDto
            {
                Id = x.Id, Name = x.Name, DisplayName = x.DisplayName, IsActive = x.IsActive,
                Permissions = x.RolePermissions
                    .Where(rp => rp.Permission.IsActive && !rp.Permission.IsRemoved)
                    .OrderBy(rp => rp.Permission.Code)
                    .Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id, Code = rp.Permission.Code, Title = rp.Permission.Title
                    }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);

        return role is null ? ResultDto<RoleDetailsDto>.Fail("نقش موردنظر پیدا نشد.", 404) : ResultDto<RoleDetailsDto>.Ok(role);
    }

    public async Task<ResultOfList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _database.Permissions.AsNoTracking()
            .Where(x => x.IsActive && !x.IsRemoved)
            .OrderBy(x => x.Code)
            .Select(x => new PermissionDto { Id = x.Id, Code = x.Code, Title = x.Title })
            .ToListAsync(cancellationToken);
        return ResultOfList<PermissionDto>.Ok(permissions, permissions.Count);
    }

    public async Task<ResultDto<RoleDetailsDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var displayName = request.DisplayName.Trim();

        if (await _database.Roles.AnyAsync(x => x.Name == name && !x.IsRemoved, cancellationToken))
            return ResultDto<RoleDetailsDto>.Fail("نقشی با این نام از قبل وجود دارد.", 409);

        var role = new Role { Name = name, DisplayName = displayName };
        _database.Roles.Add(role);
        await _database.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(role.Id, cancellationToken);
    }

    public async Task<ResultDto<RoleDetailsDto>> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var displayName = request.DisplayName.Trim();

        var role = await _database.Roles.SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);
        if (role is null)
            return ResultDto<RoleDetailsDto>.Fail("نقش موردنظر پیدا نشد.", 404);

        role.DisplayName = displayName;
        await _database.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ResultDto> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _database.Roles.AnyAsync(x => x.Id == id && !x.IsRemoved, cancellationToken))
            return ResultDto.Fail("نقش موردنظر پیدا نشد.", 404);

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        var validIds = await _database.Permissions
            .Where(x => permissionIds.Contains(x.Id) && x.IsActive && !x.IsRemoved)
            .Select(x => x.Id).ToListAsync(cancellationToken);

        if (validIds.Count != permissionIds.Length)
            return ResultDto.Fail("یک یا چند مجوز معتبر نیستند.");

        var current = await _database.RolePermissions.Where(x => x.RoleId == id).ToListAsync(cancellationToken);
        _database.RolePermissions.RemoveRange(current);

        foreach (var permissionId in validIds)
            _database.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = permissionId });

        await _database.SaveChangesAsync(cancellationToken);
        return ResultDto.Ok("مجوزهای نقش با موفقیت به‌روزرسانی شد.");
    }
}


    public async Task<ResultDto> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var role = await _database.Roles
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);

        if (role is null)
            return ResultDto.Fail("نقش موردنظر پیدا نشد.", 404);

        role.IsActive = isActive;
        await _database.SaveChangesAsync(cancellationToken);

        return ResultDto.Ok(isActive ? "نقش فعال شد." : "نقش غیرفعال شد.");
    }

    public async Task<ResultDto> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await _database.Roles
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);

        if (role is null)
            return ResultDto.Fail("نقش موردنظر پیدا نشد.", 404);

        var hasUsers = await _database.UserRoles
            .AnyAsync(x => x.RoleId == id, cancellationToken);

        if (hasUsers)
            return ResultDto.Fail("این نقش به کاربر اختصاص داده شده و قابل حذف نیست.", 409);

        role.IsRemoved = true;
        role.IsActive = false;

        await _database.SaveChangesAsync(cancellationToken);
        return ResultDto.Ok("نقش با موفقیت حذف شد.");
    }
}