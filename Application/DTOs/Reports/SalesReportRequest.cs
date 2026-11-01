using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Reports;

// Días de compra, ambos incluidos (igual que el filtro de Ventas).
public sealed record SalesReportRequest(
    [Required(ErrorMessage = "Indicá la fecha desde (from).")]
    DateOnly? From,
    [Required(ErrorMessage = "Indicá la fecha hasta (to).")]
    DateOnly? To);
