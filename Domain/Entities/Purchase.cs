using Domain.Interfaces;

namespace Domain.Entities;

// Una compra: varias butacas de una misma función. Los importes se guardan al comprar; si después
// cambia el precio de la función o el cargo vigente, las compras hechas no se recalculan.
public class Purchase : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ShowtimeId { get; set; }

    // Hora local del cine, igual que Showtime.StartTime.
    public DateTime CreatedAt { get; set; }

    public decimal Subtotal { get; set; }
    public Guid? FeeId { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = PurchaseStatus.Confirmed;
    public DateTime? CancelledAt { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Ticket> Tickets { get; set; } = [];

    public bool IsCancelled => Status == PurchaseStatus.Cancelled;
}
