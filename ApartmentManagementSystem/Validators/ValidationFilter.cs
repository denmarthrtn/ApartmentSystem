using ApartmentManagementSystem.Models.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApartmentManagementSystem.Validators
{
    // Runs the matching FluentValidation validator for every action argument that has one,
    // and returns 400 with the same ErrorResponse body the ExceptionMiddleware uses.
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _services;

        public ValidationFilter(IServiceProvider services)
        {
            _services = services;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var errors = new List<string>();

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (_services.GetService(validatorType) is not IValidator validator) continue;

                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
            }

            if (errors.Count > 0)
            {
                context.Result = BadRequest(errors);
                return;
            }

            await next();
        }

        public static IActionResult BadRequest(IReadOnlyCollection<string> errors) => new BadRequestObjectResult(new ErrorResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Message = errors.Count == 1 ? errors.First() : "One or more fields are invalid",
            Details = string.Join(" ", errors)
        });
    }
}
