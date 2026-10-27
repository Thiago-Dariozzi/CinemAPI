using Application.DTOs.Fees;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Security;

namespace WebApi.Controllers;

// [Authorize] de clase: el cargo vigente lo ve cualquier usuario logueado. El ABM es solo del SuperAdmin.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FeeController(IFeeService feeService) : ControllerBase
{
    // El cargo que se va a sumar a una compra nueva (el cliente lo ve antes de confirmar).
    // 204 si no hay ninguno vigente.
    [HttpGet("current")]
    [ProducesResponseType<FeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FeeResponse>> GetCurrent(CancellationToken ct) =>
        await feeService.GetCurrentAsync(ct) is { } fee ? Ok(fee) : NoContent();

    [HttpGet]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType<IReadOnlyList<FeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<FeeResponse>>> GetFees(CancellationToken ct) =>
        Ok(await feeService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType<FeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FeeResponse>> GetFee(Guid id, CancellationToken ct) =>
        Ok(await feeService.GetByIdAsync(id, ct));

    // isCurrent = true lo deja vigente y desmarca al anterior.
    [HttpPost]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType<FeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FeeResponse>> CreateFee(CreateFeeRequest request, CancellationToken ct)
    {
        var created = await feeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetFee), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateFee(Guid id, UpdateFeeRequest request, CancellationToken ct)
    {
        await feeService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteFee(Guid id, CancellationToken ct)
    {
        await feeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
