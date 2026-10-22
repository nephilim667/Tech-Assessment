using FluentValidation;

namespace WebApi.Infrastructure.Api.Common
{
    internal sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter where TRequest : class
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            TRequest request = context.Arguments.OfType<TRequest>().Single();

            var validation = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

            if (validation.IsValid)
                return await next(context);

            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .Distinct()
                        .ToArray());

            return Results.ValidationProblem(
                errors,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "validation_failed"
                });
        }
    }
}
