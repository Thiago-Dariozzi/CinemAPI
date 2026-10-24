using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Screens;

public sealed record CreateScreenRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,
    [Range(1, 100_000, ErrorMessage = "La capacidad debe ser mayor a 0.")]
    int Capacity);
