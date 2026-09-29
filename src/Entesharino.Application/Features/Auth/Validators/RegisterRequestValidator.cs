using FluentValidation;
using Entesharino.Application.Features.Auth.Models;

namespace Entesharino.Application.Features.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("نام الزامی است.")
            .MaximumLength(100).WithMessage("نام نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است.")
            .MaximumLength(100).WithMessage("نام خانوادگی نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است.")
            .Length(3, 50).WithMessage("نام کاربری باید بین ۳ تا ۵۰ کاراکتر باشد.")
            .Matches(@"^[a-zA-Z0-9_.-]+$")
            .WithMessage("نام کاربری فقط می‌تواند شامل حروف انگلیسی، عدد، نقطه، خط تیره و زیرخط باشد.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ایمیل الزامی است.")
            .EmailAddress().WithMessage("فرمت ایمیل وارد شده صحیح نیست.")
            .MaximumLength(200).WithMessage("ایمیل نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.")
            .MaximumLength(100).WithMessage("رمز عبور نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");
    }
}
