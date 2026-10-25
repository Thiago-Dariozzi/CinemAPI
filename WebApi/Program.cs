using Application.Interfaces;
using Application.Services;
using Infrastructure;
using Infrastructure.Data;
using WebApi.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        // El 500 genérico sale con título en español y sin detalles internos.
        if (context.ProblemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Title = "Ocurrió un error inesperado.";
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

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IScreenService, ScreenService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<IGenreService, GenreService>();

var app = builder.Build();

app.UseExceptionHandler();
// Con AddProblemDetails, las respuestas de error sin cuerpo (ruta inexistente, 405) también salen
// como ProblemDetails: el front siempre recibe el mismo formato.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Datos de prueba (Bogus): nunca fuera de Development.
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CinemAPIContext>();
    await DbSeeder.SeedAsync(context);

    app.MapOpenApi();
}

// Debe ir antes de MapControllers.
app.UseCors("AllowReactApp");

app.MapControllers();

await app.RunAsync();
