using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Fees;

// IsCurrent = false sobre el cargo vigente deja la API sin cargo: las compras nuevas no suman nada.
public sealed record UpdateFeeRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Name,
    [Required(ErrorMessage = "El porcentaje es obligatorio.")]
    [Range(0d, 100d, ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
    decimal? Percentage,
    [Required(ErrorMessage = "El monto fijo es obligatorio.")]
    [Range(0d, 1_000_000d, ErrorMessage = "El monto fijo debe estar entre 0 y 1.000.000.")]
    decimal? FixedAmount,
    [Required(ErrorMessage = "Indicá si el cargo es el vigente.")]
    bool? IsCurrent);
