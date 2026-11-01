namespace Domain.Entities;

/// <summary>
/// Estados de una compra. Se guardan como texto en Purchases.Status y viajan así en el JSON.
/// </summary>
public static class PurchaseStatus
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}
