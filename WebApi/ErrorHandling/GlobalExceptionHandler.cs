using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace WebApi.ErrorHandling;

// Traduce las excepciones de dominio a ProblemDetails (application/problem+json) en un solo lugar.
// El mensaje para el usuario va en "detail". Lo no contemplado sigue de largo: el middleware lo
// loguea y responde un 500 genérico, sin exponer detalles internos.
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException)
        {
            return false;
        }

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            InvalidReferenceException => (StatusCodes.Status400BadRequest, "Referencia inválida"),
            GenreConflictException or ShowtimeConflictException or UserConflictException =>
                (StatusCodes.Status409Conflict, "Conflicto"),
            _ => (StatusCodes.Status400BadRequest, "Regla de negocio no cumplida"),
        };

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
