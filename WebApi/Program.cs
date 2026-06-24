using Application.Services;
using Domain.Inerfaces;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("CinemaConnection");
builder.Services.AddDbContext<CinemAPIContext>(options =>
    options.UseSqlServer(connectionString)
);

#region Injections
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
