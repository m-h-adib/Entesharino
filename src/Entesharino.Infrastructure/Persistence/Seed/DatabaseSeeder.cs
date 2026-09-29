using Microsoft.EntityFrameworkCore;
using Entesharino.Domain.Constants;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        await SeedRolesAsync(context, cancellationToken);
        await SeedPermissionsAsync(context, cancellationToken);
        await SeedRolePermissionsAsync(context, cancellationToken);
    }

    private static async Task SeedRolesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new Role { Name = RoleNames.Admin, DisplayName = "مدیر سیستم" },
            new Role { Name = RoleNames.User, DisplayName = "کاربر" }
        };

        foreach (var role in roles)
        {
            var exists = await context.Roles.AnyAsync(
                x => x.Name == role.Name,
                cancellationToken);

            if (!exists)
                context.Roles.Add(role);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPermissionsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var permissions = new[]
        {
            new Permission { Code = PermissionCodes.UsersView, Title = "مشاهده کاربران" },
            new Permission { Code = PermissionCodes.UsersManage, Title = "مدیریت کاربران" },
            new Permission { Code = PermissionCodes.ChannelsView, Title = "مشاهده کانال‌ها" },
            new Permission { Code = PermissionCodes.ChannelsManage, Title = "مدیریت کانال‌ها" },
            new Permission { Code = PermissionCodes.PostsView, Title = "مشاهده پست‌ها" },
            new Permission { Code = PermissionCodes.PostsCreate, Title = "ایجاد پست" },
            new Permission { Code = PermissionCodes.PostsManage, Title = "مدیریت پست‌ها" },
            new Permission { Code = PermissionCodes.ReportsView, Title = "مشاهده گزارش‌ها" }
        };

        foreach (var permission in permissions)
        {
            var exists = await context.Permissions.AnyAsync(
                x => x.Code == permission.Code,
                cancellationToken);

            if (!exists)
                context.Permissions.Add(permission);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolePermissionsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var admin = await context.Roles
            .SingleAsync(x => x.Name == RoleNames.Admin, cancellationToken);

        var user = await context.Roles
            .SingleAsync(x => x.Name == RoleNames.User, cancellationToken);

        var permissions = await context.Permissions
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        var adminCodes = permissions.Keys.ToArray();

        var userCodes = new[]
        {
            PermissionCodes.ChannelsView,
            PermissionCodes.ChannelsManage,
            PermissionCodes.PostsView,
            PermissionCodes.PostsCreate,
            PermissionCodes.PostsManage
        };

        await AddMissingRolePermissionsAsync(
            context,
            admin.Id,
            adminCodes,
            permissions,
            cancellationToken);

        await AddMissingRolePermissionsAsync(
            context,
            user.Id,
            userCodes,
            permissions,
            cancellationToken);
    }

    private static async Task AddMissingRolePermissionsAsync(
        ApplicationDbContext context,
        long roleId,
        IEnumerable<string> permissionCodes,
        IReadOnlyDictionary<string, Permission> permissions,
        CancellationToken cancellationToken)
    {
        var codes = permissionCodes.Distinct().ToArray();

        var permissionIds = codes
            .Where(permissions.ContainsKey)
            .Select(code => permissions[code].Id)
            .ToArray();

        var existingIds = await context.RolePermissions
            .Where(x => x.RoleId == roleId && permissionIds.Contains(x.PermissionId))
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        var existingSet = existingIds.ToHashSet();

        foreach (var permissionId in permissionIds)
        {
            if (existingSet.Contains(permissionId))
                continue;

            context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
