using FluentValidation;
using Entesharino.Application.Features.Channels.Models;

namespace Entesharino.Application.Features.Channels.Validators;

public sealed class ChannelListRequestValidator : AbstractValidator<ChannelListRequest>
{
    public ChannelListRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("شماره صفحه باید حداقل ۱ باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("تعداد آیتم‌های صفحه باید بین ۱ تا ۱۰۰ باشد.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => x.Search is not null)
            .WithMessage("عبارت جستجو نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");
    }
}
