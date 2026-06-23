using System.ComponentModel.DataAnnotations;

namespace TaskManager.Validation;

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            foreach (var arg in context.Arguments)
            {
                if (arg is null)
                    continue;

                var results = new List<ValidationResult>();
                var validationContext = new ValidationContext(arg);

                if (!Validator.TryValidateObject(arg, validationContext, results, validateAllProperties: true))
                {
                    var errors = results
                        .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(r => r.ErrorMessage ?? "Invalid").ToArray());

                    return Results.ValidationProblem(errors);
                }
            }

            return await next(context);
        });
    }
}
