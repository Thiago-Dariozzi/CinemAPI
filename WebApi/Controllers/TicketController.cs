using Application.DTOs.Tickets;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Security;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TicketController(ITicketService ticketService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType<IReadOnlyList<TicketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TicketResponse>>> GetTickets(CancellationToken ct) =>
        Ok(await ticketService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TicketResponse>> GetTicket(Guid id, CancellationToken ct) =>
        Ok(await ticketService.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TicketResponse>> CreateTicket(CreateTicketRequest request, CancellationToken ct)
    {
        var created = await ticketService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetTicket), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTicket(Guid id, UpdateTicketRequest request, CancellationToken ct)
    {
        await ticketService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTicket(Guid id, CancellationToken ct)
    {
        await ticketService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType<IReadOnlyList<TicketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TicketResponse>>> GetByUser(Guid userId, CancellationToken ct) =>
        Ok(await ticketService.GetByUserIdAsync(userId, ct));
}
