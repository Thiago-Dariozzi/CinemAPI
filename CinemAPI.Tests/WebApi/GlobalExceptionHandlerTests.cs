using System.Text.Json;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebApiErrorHandling = global::WebApi.ErrorHandling;

namespace CinemAPI.Tests.WebApi;

public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData(typeof(NotFoundException), 404, "Recurso no encontrado")]
    [InlineData(typeof(GenreConflictException), 409, "Conflicto")]
    [InlineData(typeof(ShowtimeConflictException), 409, "Conflicto")]
    [InlineData(typeof(UserConflictException), 409, "Conflicto")]
    [InlineData(typeof(UniqueConstraintConflictException), 409, "Conflicto")]
    [InlineData(typeof(PurchaseConflictException), 409, "Conflicto")]
    [InlineData(typeof(ScreenConflictException), 409, "Conflicto")]
    [InlineData(typeof(InvalidCredentialsException), 401, "No autenticado")]
    [InlineData(typeof(ForbiddenException), 403, "Acceso denegado")]
    [InlineData(typeof(BusinessRuleException), 400, "Regla de negocio no cumplida")]
    public async Task DomainExceptions_AreWrittenAsProblemDetailsWithMessageInDetail(Type exceptionType, int status, string title)
    {
        var exception = exceptionType == typeof(NotFoundException) ? new NotFoundException("el género", Guid.Empty)
            : exceptionType == typeof(UniqueConstraintConflictException) ? new UniqueConstraintConflictException(new InvalidOperationException())
            : (Exception)Activator.CreateInstance(exceptionType, "Mensaje para el usuario.")!;
        var httpContext = NewHttpContext();

        var handled = await NewHandler(httpContext).TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(status, httpContext.Response.StatusCode);
        Assert.StartsWith("application/problem+json", httpContext.Response.ContentType, StringComparison.Ordinal);
        using var body = await ReadBodyAsync(httpContext);
        Assert.Equal(title, body.RootElement.GetProperty("title").GetString());
        Assert.Equal(exception.Message, body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnexpectedExceptions_AreNotHandled_SoTheyEndAsGeneric500()
    {
        var httpContext = NewHttpContext();

        var handled = await NewHandler(httpContext).TryHandleAsync(
            httpContext, new InvalidOperationException("detalle interno"), CancellationToken.None);

        Assert.False(handled);
    }

    private static DefaultHttpContext NewHttpContext()
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        return new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
    }

    private static WebApiErrorHandling.GlobalExceptionHandler NewHandler(HttpContext httpContext) =>
        new(httpContext.RequestServices.GetRequiredService<IProblemDetailsService>());

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(httpContext.Response.Body);
    }
}
