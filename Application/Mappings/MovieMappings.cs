using Application.DTOs.Movies;
using Domain.Entities;

namespace Application.Mappings;

public static class MovieMappings
{
    public static MovieResponse ToResponse(this Movie movie) => new(
        movie.Id,
        movie.Title,
        movie.Synopsis,
        movie.DurationMinutes,
        movie.GenreId,
        movie.ImageUrl,
        movie.ReleaseDate,
        movie.SuggestedPrice,
        movie.IsActive);

    // GetValueOrDefault: [Required] ya garantizó el valor antes de llegar al servicio.
    public static Movie ToEntity(this CreateMovieRequest request) => new()
    {
        Title = request.Title,
        Synopsis = request.Synopsis,
        DurationMinutes = request.DurationMinutes,
        GenreId = request.GenreId.GetValueOrDefault(),
        ImageUrl = request.ImageUrl ?? string.Empty,
        ReleaseDate = request.ReleaseDate,
        SuggestedPrice = request.SuggestedPrice,
    };

    public static void ApplyTo(this UpdateMovieRequest request, Movie movie)
    {
        movie.Title = request.Title;
        movie.Synopsis = request.Synopsis;
        movie.DurationMinutes = request.DurationMinutes;
        movie.GenreId = request.GenreId.GetValueOrDefault();
        movie.ImageUrl = request.ImageUrl ?? string.Empty;
        movie.ReleaseDate = request.ReleaseDate;
        movie.SuggestedPrice = request.SuggestedPrice;
    }
}
