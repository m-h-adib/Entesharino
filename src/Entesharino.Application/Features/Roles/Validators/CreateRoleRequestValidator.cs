using FluentValidation;
using Entesharino.Application.Features.Roles.Models;

namespace Entesharino.Application.Features.Roles.Validators;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام نقش الزامی است.")
            .MaximumLength(100).WithMessage("نام نقش نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("عنوان نمایشی الزامی است.")
            .MaximumLength(150).WithMessage("عنوان نمایشی نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد.");
    }
}
