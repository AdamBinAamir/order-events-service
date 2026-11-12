using Microsoft.AspNetCore.Diagnostics;
using Orders.Domain.Common;

namespace Orders.Api;

/// <summary>Turns business rule violations into 400 ProblemDetails responses.</summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Business rule violated",
                Detail = domainException.Message
            }
        });
    }
}
