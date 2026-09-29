using FluentValidation;
using Entesharino.Application.Features.Roles.Models;

namespace Entesharino.Application.Features.Roles.Validators;

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("عنوان نمایشی الزامی است.")
            .MaximumLength(150).WithMessage("عنوان نمایشی نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد.");
    }
}
