using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace WebApi.ErrorHandling;

// Traduce las excepciones de dominio a respuestas HTTP en un solo lugar. Reproduce las respuestas
// que antes armaba cada controller: 404 como el ProblemDetails de NotFound() con [ApiController]
// y 409 con el mensaje como cuerpo, negociado igual que Conflict(mensaje). Lo no contemplado
// sigue de largo y termina en un 500 (el middleware lo loguea).
internal sealed class GlobalExceptionHandler(
    IActionResultExecutor<ObjectResult> resultExecutor,
    ProblemDetailsFactory problemDetailsFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var result = exception switch
        {
            NotFoundException => NotFoundProblem(httpContext),
            GenreConflictException or ShowtimeConflictException or UserConflictException =>
                new ObjectResult(exception.Message) { StatusCode = StatusCodes.Status409Conflict },
            _ => null,
        };

        if (result is null)
        {
            return false;
        }

        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        await resultExecutor.ExecuteAsync(actionContext, result);
        return true;
    }

    private ObjectResult NotFoundProblem(HttpContext httpContext)
    {
        var problem = problemDetailsFactory.CreateProblemDetails(httpContext, StatusCodes.Status404NotFound);
        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status404NotFound,
            ContentTypes = { "application/problem+json", "application/problem+xml" },
        };
    }
}
