using Application.DTOs.Reports;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Security;

namespace WebApi.Controllers;

// Plural a propósito: la ruta pedida es /api/reports.
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.Staff)]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    // GET api/reports/sales?from=2026-10-01&to=2026-10-31 — días de compra, ambos incluidos.
    [HttpGet("sales")]
    [ProducesResponseType<SalesReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SalesReportResponse>> GetSales([FromQuery] SalesReportRequest request, CancellationToken ct) =>
        Ok(await reportService.GetSalesAsync(request, ct));

    // GET api/reports/occupancy?date=2026-10-27 — funciones que empiezan ese día.
    [HttpGet("occupancy")]
    [ProducesResponseType<OccupancyReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OccupancyReportResponse>> GetOccupancy([FromQuery] OccupancyReportRequest request, CancellationToken ct) =>
        Ok(await reportService.GetOccupancyAsync(request, ct));
}
