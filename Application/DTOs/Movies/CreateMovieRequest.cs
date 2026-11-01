using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Movies;

// Guid?/DateTime? + [Required]: con el tipo a secas, [Required] no detecta que falte.
public sealed record CreateMovieRequest(
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, ErrorMessage = "El título no puede superar los 150 caracteres.")]
    string Title,
    [StringLength(5000, ErrorMessage = "La sinopsis no puede superar los 5000 caracteres.")]
    string? Synopsis,
    [Range(1, 1000, ErrorMessage = "La duración debe estar entre 1 y 1000 minutos.")]
    int DurationMinutes,
    [Required(ErrorMessage = "El género es obligatorio.")]
    Guid? GenreId,
    [Url(ErrorMessage = "La URL de la imagen no es válida.")]
    [StringLength(500, ErrorMessage = "La URL de la imagen no puede superar los 500 caracteres.")]
    string? ImageUrl,
    [Required(ErrorMessage = "La fecha de estreno es obligatoria.")]
    DateTime? ReleaseDate,
    [Range(0.01d, 1_000_000d, ErrorMessage = "El precio sugerido debe ser mayor a 0 y no superar 1.000.000.")]
    decimal? SuggestedPrice,
    bool IsShowing = true);
