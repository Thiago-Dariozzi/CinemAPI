using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Genres;

public sealed record CreateGenreRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);
