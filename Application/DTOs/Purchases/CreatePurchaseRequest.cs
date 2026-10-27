using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Purchases;

// El usuario sale del token y los precios de la función y del cargo vigente: el cliente solo elige
// función y butacas.
public sealed record CreatePurchaseRequest(
    [Required(ErrorMessage = "La función es obligatoria.")]
    Guid? ShowtimeId,
    [Required(ErrorMessage = "Elegí al menos una butaca.")]
    IReadOnlyList<SeatRequest>? Seats);
