using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;

namespace Entesharino.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IDatabaseContext _database;

    public PermissionAuthorizationHandler(IDatabaseContext database)
    {
        _database = database;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdValue, out var userId))
            return;

        var hasPermission = await _database.RolePermissions
            .AnyAsync(
                x => x.Permission.Code == requirement.Permission &&
                     x.Permission.IsActive &&
                     !x.Permission.IsRemoved &&
                     x.Role.IsActive &&
                     !x.Role.IsRemoved &&
                     x.Role.UserRoles.Any(ur => ur.UserId == userId),
                CancellationToken.None);

        if (hasPermission)
            context.Succeed(requirement);
    }
}
