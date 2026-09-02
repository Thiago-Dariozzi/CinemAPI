using System;

namespace Domain.Entities;

// Una función: una película en una sala a una hora. No guarda la hora de fin, se
// calcula con MovieId.DurationMinutes + buffer de limpieza (ver ShowtimeService).
public class Showtime
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Guid ScreenId { get; set; }
    public DateTime StartTime { get; set; }

    // Precio de venta de esta función puntual; no tiene por qué coincidir con
    // Movie.SuggestedPrice (ese es solo el valor sugerido al crear).
    public decimal Price { get; set; }

    // Soft delete: en false significa borrada, no cancelada/pausada.
    public bool IsActive { get; set; } = true;
}
