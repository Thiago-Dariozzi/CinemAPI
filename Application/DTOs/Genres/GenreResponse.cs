namespace Application.DTOs.Genres;

public sealed record GenreResponse(Guid Id, string Name, bool IsActive);
