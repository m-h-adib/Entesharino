using FluentValidation;
using Entesharino.Application.Features.Roles.Models;

namespace Entesharino.Application.Features.Roles.Validators;

public sealed class SetRolePermissionsRequestValidator : AbstractValidator<SetRolePermissionsRequest>
{
    public SetRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds).NotNull();
        RuleForEach(x => x.PermissionIds).GreaterThan(0);
    }
}
