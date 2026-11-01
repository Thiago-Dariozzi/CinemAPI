using Domain.Interfaces;

namespace Domain.Entities;

// Una función: una película en una sala a una hora. No guarda la hora de fin, se
// calcula con la duración de la película + CleaningBufferMinutes.
public class Showtime : IEntity
{
    // Minutos de limpieza que se suman a la duración de la película para calcular el
    // tramo real que ocupa una función en la sala.
    public const int CleaningBufferMinutes = 15;

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
