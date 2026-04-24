using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dashboard.Api.Middleware;

/// <summary>
/// Runs FluentValidation on all registered action arguments and turns failures into 400s.
/// Avoids the deprecated FluentValidation.AspNetCore auto-wiring.
/// </summary>
public sealed class ValidationFilter(IServiceProvider sp) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, argument) in context.ActionArguments)
        {
            if (argument is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = sp.GetService(validatorType) as IValidator;
            if (validator is null) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
                return;
            }
        }
        await next();
    }
}
