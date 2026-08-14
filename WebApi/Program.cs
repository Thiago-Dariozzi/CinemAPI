using Application.Services;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // <-- La URL exacta de tu frontend
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var connectionString = builder.Configuration.GetConnectionString("CinemaConnection");
builder.Services.AddDbContext<CinemAPIContext>(options =>
    options.UseSqlServer(connectionString)
);

#region Injections
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<ScreenService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IScreenRepository, ScreenRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
#endregion

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CinemAPIContext>();
    await Infrastructure.Data.DbSeeder.SeedAsync(context);
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 2. ACTIVAMOS EL CORS (Debe ir antes de MapControllers)
app.UseCors("AllowReactApp");

app.MapControllers();

app.Run();
