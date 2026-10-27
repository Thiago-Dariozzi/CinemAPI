using Application.DTOs.Purchases;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Security;

namespace WebApi.Controllers;

// Comprar es solo de clientes. Ver una compra o cancelarla: el dueño o el staff (lo decide el servicio).
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PurchaseController(IPurchaseService purchaseService) : ControllerBase
{
    // GET api/purchase?movieId=&showtimeId=&from=2026-10-01&to=2026-10-31&status=Confirmed
    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType<IReadOnlyList<PurchaseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PurchaseResponse>>> GetPurchases(
        [FromQuery] PurchaseSearchRequest filter, CancellationToken ct) =>
        Ok(await purchaseService.SearchAsync(filter, ct));

    [HttpGet("mine")]
    [Authorize(Policy = Policies.ClientOnly)]
    [ProducesResponseType<IReadOnlyList<PurchaseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PurchaseResponse>>> GetMine(CancellationToken ct) =>
        Ok(await purchaseService.GetMineAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PurchaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PurchaseResponse>> GetPurchase(Guid id, CancellationToken ct) =>
        Ok(await purchaseService.GetByIdAsync(id, ct));

    // 409 si alguna butaca ya está vendida (incluida la carrera de dos compras simultáneas).
    [HttpPost]
    [Authorize(Policy = Policies.ClientOnly)]
    [ProducesResponseType<PurchaseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PurchaseResponse>> CreatePurchase(CreatePurchaseRequest request, CancellationToken ct)
    {
        var created = await purchaseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetPurchase), new { id = created.Id }, created);
    }

    // POST y no DELETE: la compra no se borra, cambia de estado (y libera las butacas).
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<PurchaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PurchaseResponse>> CancelPurchase(Guid id, CancellationToken ct) =>
        Ok(await purchaseService.CancelAsync(id, ct));
}
