using Domain.Interfaces;

namespace Domain.Entities;

// Una entrada: una butaca de una función, dentro de una compra. ShowtimeId repite el de la compra para
// que el índice único de butaca (función, fila, número) no necesite un join.
public class Ticket : IEntity
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public Guid ShowtimeId { get; set; }
    public int SeatRow { get; set; }
    public int SeatNumber { get; set; }

    // Showtime.Price al momento de comprar.
    public decimal Price { get; set; }

    // Cancelada junto con su compra: la butaca vuelve a quedar libre.
    public bool IsCancelled { get; set; }

    public bool IsActive { get; set; } = true;
}
