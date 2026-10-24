using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TicketController(ITicketService ticketService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTickets(CancellationToken ct)
    {
        return Ok(await ticketService.GetAllAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        return Ok(await ticketService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] Ticket ticket, CancellationToken ct)
    {
        var created = await ticketService.AddAsync(ticket, ct);
        return CreatedAtAction(nameof(GetTicket), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] Ticket ticket, CancellationToken ct)
    {
        if (id != ticket.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del ticket.");
        }

        await ticketService.UpdateAsync(ticket, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id, CancellationToken ct)
    {
        await ticketService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId, CancellationToken ct)
    {
        return Ok(await ticketService.GetByUserIdAsync(userId, ct));
    }
}
