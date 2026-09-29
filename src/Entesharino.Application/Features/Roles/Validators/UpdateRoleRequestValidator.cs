using FluentValidation;
using Entesharino.Application.Features.Roles.Models;

namespace Entesharino.Application.Features.Roles.Validators;

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
    }
}
