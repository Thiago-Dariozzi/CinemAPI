namespace Application.DTOs.Movies;

public sealed record MovieResponse(
    Guid Id,
    string Title,
    string? Synopsis,
    int DurationMinutes,
    Guid GenreId,
    string ImageUrl,
    DateTime ReleaseDate,
    decimal? SuggestedPrice,
    bool IsShowing,
    bool IsActive);
