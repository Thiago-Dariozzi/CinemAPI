using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Showtimes;

// Guid?/DateTime? + [Required]: con el tipo a secas, [Required] no detecta que falte.
public sealed record CreateShowtimeRequest(
    [Required(ErrorMessage = "La película es obligatoria.")]
    Guid? MovieId,
    [Required(ErrorMessage = "La sala es obligatoria.")]
    Guid? ScreenId,
    [Required(ErrorMessage = "El horario es obligatorio.")]
    DateTime? StartTime,
    [Range(0, 1_000_000, ErrorMessage = "El precio no puede ser negativo.")]
    decimal Price);
