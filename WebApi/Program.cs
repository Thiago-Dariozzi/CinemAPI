using Application.Services;
using Infrastructure;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<ScreenService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<ShowtimeService>();
builder.Services.AddScoped<GenreService>();

var app = builder.Build();

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
