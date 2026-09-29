using FluentValidation;
using Entesharino.Application.Features.Users.Models;

namespace Entesharino.Application.Features.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Username).NotEmpty().Length(3, 50).Matches(@"^[a-zA-Z0-9_.-]+$");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.RoleId).GreaterThan(0).When(x => x.RoleId.HasValue);
    }
}
