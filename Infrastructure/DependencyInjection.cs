using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CinemaConnection")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'CinemaConnection'.");

        services.AddDbContext<CinemAPIContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CinemAPIContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IScreenRepository, ScreenRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();

        return services;
    }
}
