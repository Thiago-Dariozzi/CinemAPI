using Application;
using Infrastructure;
using Infrastructure.Data;
using WebApi.ErrorHandling;
using WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.AddOperationTransformer<BearerSecurityTransformer>();
});
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        // El 500 genérico sale con título en español y sin detalles internos. El 401/403 de [Authorize]
        // llega sin detalle (el de las excepciones de dominio ya trae el suyo y no se pisa).
        var problem = context.ProblemDetails;
        switch (problem.Status)
        {
            case StatusCodes.Status500InternalServerError:
                problem.Title = "Ocurrió un error inesperado.";
                break;
            case StatusCodes.Status401Unauthorized when problem.Detail is null:
                problem.Title = "No autenticado";
                problem.Detail = "Iniciá sesión para continuar (o tu sesión venció).";
                break;
            case StatusCodes.Status403Forbidden when problem.Detail is null:
                problem.Title = "Acceso denegado";
                problem.Detail = "Tu usuario no tiene permiso para esta operación.";
                break;
        }
    });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
// Con AddProblemDetails, las respuestas de error sin cuerpo (ruta inexistente, 405) también salen
// como ProblemDetails: el front siempre recibe el mismo formato.
app.UseStatusCodePages();

// Rehash de contraseñas viejas y SuperAdmin inicial en todos los ambientes; datos de prueba (Bogus y
// usuarios demo) nunca fuera de Development.
await DataInitializer.InitializeAsync(app.Services, seedDemoData: app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS antes de la autenticación (el preflight no lleva token) y todo antes de MapControllers.
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
