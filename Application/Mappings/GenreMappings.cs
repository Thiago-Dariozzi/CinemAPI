using Application.DTOs.Genres;
using Domain.Entities;

namespace Application.Mappings;

public static class GenreMappings
{
    public static GenreResponse ToResponse(this Genre genre) => new(genre.Id, genre.Name, genre.IsActive);
}
