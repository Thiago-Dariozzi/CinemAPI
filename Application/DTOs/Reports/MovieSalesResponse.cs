namespace Application.DTOs.Reports;

// TicketRevenue: lo cobrado por las entradas; FeesCollected: el cargo por servicio; TotalRevenue: la suma.
public sealed record MovieSalesResponse(
    Guid MovieId,
    string MovieTitle,
    int Purchases,
    int TicketsSold,
    decimal TicketRevenue,
    decimal FeesCollected,
    decimal TotalRevenue);
