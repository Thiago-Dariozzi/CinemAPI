using Domain.Interfaces;

namespace Domain.Entities;

// Cargo por servicio que se suma a cada compra (la "comisión"). A lo sumo uno vigente a la vez.
public class Fee : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // 10 = 10 % del subtotal de la compra.
    public decimal Percentage { get; set; }

    // Monto fijo por compra (no por entrada).
    public decimal FixedAmount { get; set; }

    // "Vigente": es el que se aplica a las compras nuevas. No confundir con IsActive (baja lógica).
    public bool IsCurrent { get; set; }

    public bool IsActive { get; set; } = true;

    // Redondeo comercial a centavos: 0,005 sube.
    public decimal CalculateFor(decimal subtotal) =>
        Math.Round(subtotal * Percentage / 100m + FixedAmount, 2, MidpointRounding.AwayFromZero);
}
