using FluentValidation;
using Entesharino.Application.Features.Auth.Models;

namespace Entesharino.Application.Features.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری یا ایمیل الزامی است.")
            .MaximumLength(200).WithMessage("نام کاربری یا ایمیل نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.");
    }
}
