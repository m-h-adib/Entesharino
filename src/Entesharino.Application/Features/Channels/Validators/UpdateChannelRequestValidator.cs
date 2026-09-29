using FluentValidation;
using Entesharino.Application.Features.Channels.Models;

namespace Entesharino.Application.Features.Channels.Validators;

public sealed class UpdateChannelRequestValidator : AbstractValidator<UpdateChannelRequest>
{
    public UpdateChannelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام کانال الزامی است.")
            .MaximumLength(200).WithMessage("نام کانال نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("شناسه کانال الزامی است.")
            .MaximumLength(200).WithMessage("شناسه کانال نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null)
            .WithMessage("توضیحات نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.");
    }
}
