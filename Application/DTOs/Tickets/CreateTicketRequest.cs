using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tickets;

// Guid?/DateTime? + [Required]: con el tipo a secas, [Required] no detecta que falte.
public sealed record CreateTicketRequest(
    [Required(ErrorMessage = "La película es obligatoria.")]
    Guid? MovieId,
    [Required(ErrorMessage = "La sala es obligatoria.")]
    Guid? ScreenId,
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    Guid? UserId,
    [Required(ErrorMessage = "La fecha de compra es obligatoria.")]
    DateTime? BuyDate,
    [Range(0, 1_000_000, ErrorMessage = "El precio debe estar entre 0 y 1.000.000.")]
    decimal FinalPrice);
