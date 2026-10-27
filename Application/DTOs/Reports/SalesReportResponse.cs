namespace Application.DTOs.Reports;

// Solo compras confirmadas; TicketsCancelled cuenta las entradas de compras del período que se cancelaron.
public sealed record SalesReportResponse(
    DateOnly From,
    DateOnly To,
    int Purchases,
    int TicketsSold,
    int TicketsCancelled,
    decimal TicketRevenue,
    decimal FeesCollected,
    decimal TotalRevenue,
    IReadOnlyList<MovieSalesResponse> ByMovie);
