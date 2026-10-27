using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<IScreenService, ScreenService>();
        services.AddScoped<IShowtimeService, ShowtimeService>();
        services.AddScoped<IGenreService, GenreService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFeeService, FeeService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IReportService, ReportService>();

        // La hora de las reglas de compra y cancelación; los tests usan una fija.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
