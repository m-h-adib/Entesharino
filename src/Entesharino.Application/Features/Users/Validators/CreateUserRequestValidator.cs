using FluentValidation;
using Entesharino.Application.Features.Users.Models;

namespace Entesharino.Application.Features.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("نام الزامی است.")
            .MaximumLength(100).WithMessage("نام نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است.")
            .MaximumLength(100).WithMessage("نام خانوادگی نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است.")
            .Length(3, 50).WithMessage("نام کاربری باید بین 3 تا 50 کاراکتر باشد.")
            .Matches(@"^[a-zA-Z0-9_.-]+$")
            .WithMessage("نام کاربری فقط می‌تواند شامل حروف انگلیسی، عدد، نقطه، خط تیره و زیرخط باشد.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ایمیل الزامی است.")
            .EmailAddress().WithMessage("فرمت ایمیل معتبر نیست.")
            .MaximumLength(200).WithMessage("ایمیل نمی‌تواند بیشتر از 200 کاراکتر باشد.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل 8 کاراکتر باشد.")
            .MaximumLength(100).WithMessage("رمز عبور نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.RoleId)
            .GreaterThan(0)
            .When(x => x.RoleId.HasValue)
            .WithMessage("شناسه نقش باید بزرگ‌تر از صفر باشد.");
    }
}
