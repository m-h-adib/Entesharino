using FluentValidation;
using Entesharino.Application.Features.Channels.Models;

namespace Entesharino.Application.Features.Channels.Validators;

public sealed class UpdateChannelConnectionRequestValidator : AbstractValidator<UpdateChannelConnectionRequest>
{
    public UpdateChannelConnectionRequestValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("توکن دسترسی الزامی است.")
            .MaximumLength(4000).WithMessage("توکن دسترسی نمی‌تواند بیشتر از ۴۰۰۰ کاراکتر باشد.");

        RuleFor(x => x.RefreshToken)
            .MaximumLength(4000)
            .When(x => x.RefreshToken is not null)
            .WithMessage("توکن نوسازی نمی‌تواند بیشتر از ۴۰۰۰ کاراکتر باشد.");
    }
}
