using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Movies;

// Guid? + [Required]: con Guid a secas, [Required] no detecta que falte.
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
    string? ImageUrl,
    DateTime ReleaseDate,
    [Range(0, 1_000_000, ErrorMessage = "El precio sugerido no puede ser negativo.")]
    decimal? SuggestedPrice,
    bool IsShowing = true);
