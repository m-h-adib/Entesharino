using FluentValidation;
using Entesharino.Application.Features.Users.Models;

namespace Entesharino.Application.Features.Users.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("نام الزامی است.")
            .MaximumLength(100).WithMessage("نام نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است.")
            .MaximumLength(100).WithMessage("نام خانوادگی نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ایمیل الزامی است.")
            .EmailAddress().WithMessage("فرمت ایمیل وارد شده صحیح نیست.")
            .MaximumLength(200).WithMessage("ایمیل نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");
    }
}
