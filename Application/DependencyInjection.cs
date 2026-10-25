using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<IScreenService, ScreenService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IShowtimeService, ShowtimeService>();
        services.AddScoped<IGenreService, GenreService>();

        return services;
    }
}
