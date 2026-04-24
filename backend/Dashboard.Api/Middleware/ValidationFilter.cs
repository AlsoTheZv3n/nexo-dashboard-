using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dashboard.Api.Middleware;

/// <summary>
/// Runs FluentValidation on all registered action arguments and turns failures into 400s.
/// Avoids the deprecated FluentValidation.AspNetCore auto-wiring. Constructs a typed
/// ValidationContext&lt;T&gt; via reflection so that rules using <c>Matches()</c> /
/// <c>Must()</c> on non-null properties behave identically to the library defaults.
/// </summary>
public sealed class ValidationFilter(IServiceProvider sp) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, argument) in context.ActionArguments)
        {
            if (argument is null) continue;
            var argType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argType);
            var validator = sp.GetService(validatorType) as IValidator;
            if (validator is null) continue;

            var contextType = typeof(ValidationContext<>).MakeGenericType(argType);
            var validationContext = (IValidationContext)Activator.CreateInstance(contextType, argument)!;

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
