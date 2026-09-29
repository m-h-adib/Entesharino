using FluentValidation;
using Entesharino.Application.Features.Channels.Models;
using Entesharino.Domain.Enums;

namespace Entesharino.Application.Features.Channels.Validators;

public sealed class CreateChannelRequestValidator : AbstractValidator<CreateChannelRequest>
{
    public CreateChannelRequestValidator()
    {
        RuleFor(x => x.Platform)
            .IsInEnum()
            .WithMessage("پلتفرم انتخاب‌شده معتبر نیست.");

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

        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("توکن دسترسی الزامی است.")
            .MaximumLength(4000).WithMessage("توکن دسترسی نمی‌تواند بیشتر از ۴۰۰۰ کاراکتر باشد.");

        RuleFor(x => x.RefreshToken)
            .MaximumLength(4000)
            .When(x => x.RefreshToken is not null)
            .WithMessage("توکن نوسازی نمی‌تواند بیشتر از ۴۰۰۰ کاراکتر باشد.");
    }
}
