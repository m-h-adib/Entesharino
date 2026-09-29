using FluentValidation;
using Entesharino.Application.Features.Users.Models;

namespace Entesharino.Application.Features.Users.Validators;

public sealed class UserListRequestValidator : AbstractValidator<UserListRequest>
{
    public UserListRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("شماره صفحه باید حداقل ۱ باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("تعداد آیتم‌های صفحه باید بین ۱ تا ۱۰۰ باشد.");

        RuleFor(x => x.Search)
            .MaximumLength(100)
            .When(x => x.Search is not null)
            .WithMessage("عبارت جستجو نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");
    }
}
