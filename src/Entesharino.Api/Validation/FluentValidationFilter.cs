using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Entesharino.Application.Common.Models;

namespace Entesharino.Api.Validation;

public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var cancellationToken = context.HttpContext.RequestAborted;
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, cancellationToken);

            foreach (var failure in result.Errors)
            {
                if (string.IsNullOrWhiteSpace(failure.PropertyName))
                    continue;

                if (!errors.TryGetValue(failure.PropertyName, out var messages))
                {
                    messages = [];
                    errors[failure.PropertyName] = messages;
                }

                if (!messages.Contains(failure.ErrorMessage))
                    messages.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            var result = ResultDto.Fail(
                "اطلاعات وارد شده معتبر نیست.",
                StatusCodes.Status400BadRequest,
                errors.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase));

            context.Result = new ObjectResult(result)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

            return;
        }

        await next();
    }
}
