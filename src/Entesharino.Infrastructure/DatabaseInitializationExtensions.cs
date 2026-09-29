using Microsoft.EntityFrameworkCore;
using Entesharino.Domain.Constants;
using Entesharino.Domain.Entities;
using Entesharino.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Entesharino.Infrastructure;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        await SeedRolesAndPermissionsAsync(db, cancellationToken);
    }

    private static async Task SeedRolesAndPermissionsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var permissions = new[]
        {
            new Permission { Code = PermissionCodes.UsersView, Title = "مشاهده کاربران" },
            new Permission { Code = PermissionCodes.UsersManage, Title = "مدیریت کاربران" },
            new Permission { Code = PermissionCodes.RolesView, Title = "مشاهده نقش‌ها و دسترسی‌ها" },
            new Permission { Code = PermissionCodes.RolesManage, Title = "مدیریت نقش‌ها و دسترسی‌ها" },
            new Permission { Code = PermissionCodes.ChannelsView, Title = "مشاهده کانال‌ها" },
            new Permission { Code = PermissionCodes.ChannelsManage, Title = "مدیریت کانال‌ها" },
            new Permission { Code = PermissionCodes.PostsView, Title = "مشاهده پست‌ها" },
            new Permission { Code = PermissionCodes.PostsCreate, Title = "ایجاد پست" },
            new Permission { Code = PermissionCodes.PostsManage, Title = "مدیریت پست‌ها" },
            new Permission { Code = PermissionCodes.ReportsView, Title = "مشاهده گزارش‌ها" }
        };

        var existingPermissions = await db.Permissions
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var permission in permissions)
        {
            if (!existingPermissions.TryGetValue(permission.Code, out var existing))
            {
                db.Permissions.Add(permission);
                existingPermissions[permission.Code] = permission;
            }
            else if (existing.IsRemoved)
            {
                existing.IsRemoved = false;
                existing.IsActive = true;
                existing.Title = permission.Title;
            }
        }

        var roles = new[]
        {
            new { Name = RoleNames.Admin, DisplayName = "مدیر سیستم" },
            new { Name = RoleNames.User, DisplayName = "کاربر" }
        };

        var existingRoles = await db.Roles
            .Include(x => x.RolePermissions)
            .ToDictionaryAsync(x => x.Name, cancellationToken);

        foreach (var roleDefinition in roles)
        {
            if (!existingRoles.TryGetValue(roleDefinition.Name, out var role))
            {
                role = new Role
                {
                    Name = roleDefinition.Name,
                    DisplayName = roleDefinition.DisplayName
                };

                db.Roles.Add(role);
                existingRoles[role.Name] = role;
            }
            else if (role.IsRemoved)
            {
                role.IsRemoved = false;
                role.IsActive = true;
                role.DisplayName = roleDefinition.DisplayName;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var adminRole = existingRoles[RoleNames.Admin];
        var allPermissions = existingPermissions.Values.ToList();

        var existingAdminPermissionIds = adminRole.RolePermissions
            .Select(x => x.PermissionId)
            .ToHashSet();

        foreach (var permission in allPermissions)
        {
            if (!existingAdminPermissionIds.Contains(permission.Id))
            {
                adminRole.RolePermissions.Add(new RolePermission
                {
                    PermissionId = permission.Id
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
