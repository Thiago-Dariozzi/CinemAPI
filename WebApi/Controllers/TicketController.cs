using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TicketController : ControllerBase
{
    private readonly TicketService _ticketService;

    public TicketController(TicketService ticketService)
    {
        _ticketService = ticketService;
    }

    // GET: api/ticket
    [HttpGet]
    public async Task<IActionResult> GetTickets(CancellationToken ct)
    {
        var tickets = await _ticketService.GetAllAsync(ct);
        return Ok(tickets);
    }

    // GET: api/ticket/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        var ticket = await _ticketService.GetByIdAsync(id, ct);
        if (ticket == null)
        {
            return NotFound();
        }
        return Ok(ticket);
    }

    // POST: api/ticket
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] Ticket ticket, CancellationToken ct)
    {
        var created = await _ticketService.AddAsync(ticket, ct);
        return CreatedAtAction(nameof(GetTicket), new { id = created.Id }, created);
    }

    // PUT: api/ticket/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] Ticket ticket, CancellationToken ct)
    {
        if (id != ticket.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del ticket.");
        }

        var existing = await _ticketService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _ticketService.UpdateAsync(ticket, ct);
        return NoContent();
    }

    // DELETE: api/ticket/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id, CancellationToken ct)
    {
        var existing = await _ticketService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _ticketService.DeleteAsync(id, ct);
        return NoContent();
    }
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId, CancellationToken ct)
    {
        var tickets = await _ticketService.GetByUserIdAsync(userId, ct);
        return Ok(tickets);
    }
}
