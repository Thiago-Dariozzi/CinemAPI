using Application.DTOs.Fees;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

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
}
