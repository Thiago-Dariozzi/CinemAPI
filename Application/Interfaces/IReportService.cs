using Application.DTOs.Reports;

namespace Application.Interfaces;

public interface IReportService
{
    Task<SalesReportResponse> GetSalesAsync(SalesReportRequest request, CancellationToken ct);
    Task<OccupancyReportResponse> GetOccupancyAsync(OccupancyReportRequest request, CancellationToken ct);
}
