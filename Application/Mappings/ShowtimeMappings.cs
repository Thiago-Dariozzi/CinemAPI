using Application.DTOs.Showtimes;
using Domain.Entities;

namespace Application.Mappings;

public static class ShowtimeMappings
{
    public static ShowtimeResponse ToResponse(this Showtime showtime) =>
        new(showtime.Id, showtime.MovieId, showtime.ScreenId, showtime.StartTime, showtime.Price, showtime.IsActive);

    // GetValueOrDefault: [Required] ya garantizó los valores antes de llegar al servicio.
    public static Showtime ToEntity(this CreateShowtimeRequest request) => new()
    {
        MovieId = request.MovieId.GetValueOrDefault(),
        ScreenId = request.ScreenId.GetValueOrDefault(),
        StartTime = request.StartTime.GetValueOrDefault(),
        Price = request.Price,
    };

    // Devuelve una copia con los datos nuevos para validar el solapamiento antes de tocar la
    // entidad trackeada.
    public static Showtime ToEntity(this UpdateShowtimeRequest request, Guid id) => new()
    {
        Id = id,
        MovieId = request.MovieId.GetValueOrDefault(),
        ScreenId = request.ScreenId.GetValueOrDefault(),
        StartTime = request.StartTime.GetValueOrDefault(),
        Price = request.Price,
    };
}
