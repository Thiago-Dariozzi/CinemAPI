using System;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Entities;
 
namespace Controllers.TicketController;
 
[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
    private readonly TicketService _ticketService;
 
    public TicketController(TicketService ticketService)
    {
        _ticketService = ticketService;
    }
 
    // GET: api/ticket
    [HttpGet]
    public async Task<IActionResult> GetTickets()
    {
        var tickets = await _ticketService.GetAll();
        return Ok(tickets);
    }
 
    // GET: api/ticket/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var ticket = await _ticketService.GetById(id);
        if (ticket == null)
        {
            return NotFound();
        }
        return Ok(ticket);
    }
 
    // POST: api/ticket
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] Ticket ticket)
    {
        var created = await _ticketService.Add(ticket);
        return CreatedAtAction(nameof(GetTicket), new { id = created.Id }, created);
    }
 
    // PUT: api/ticket/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] Ticket ticket)
    {
        if (id != ticket.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del ticket.");
        }
 
        var existing = await _ticketService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }
 
        await _ticketService.Update(ticket);
        return NoContent();
    }
 
    // DELETE: api/ticket/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        var existing = await _ticketService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }
 
        await _ticketService.Delete(id);
        return NoContent();
    }
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var tickets = await _ticketService.GetByUserId(userId);
        return Ok(tickets);
    }
}
 