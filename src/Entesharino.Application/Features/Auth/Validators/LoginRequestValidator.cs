using FluentValidation;
using Entesharino.Application.Features.Auth.Models;

namespace Entesharino.Application.Features.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty();
    }
}
