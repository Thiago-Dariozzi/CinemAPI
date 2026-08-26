using System;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShowtimeController : ControllerBase
{
    private readonly ShowtimeService _showtimeService;

    public ShowtimeController(ShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShowtimes()
    {
        var showtimes = await _showtimeService.GetAll();
        return Ok(showtimes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShowtime(Guid id)
    {
        var showtime = await _showtimeService.GetById(id);
        if (showtime == null)
        {
            return NotFound();
        }
        return Ok(showtime);
    }

    [HttpGet("movie/{movieId}")]
    public async Task<IActionResult> GetByMovie(Guid movieId)
    {
        var showtimes = await _showtimeService.GetByMovieId(movieId);
        return Ok(showtimes);
    }

    // GET api/showtime/screen/{screenId}?date=2026-08-27
    // Horarios ocupados de una sala en una fecha puntual, para que el frontend sepa
    // qué franjas ya están tomadas al armar una función nueva.
    [HttpGet("screen/{screenId}")]
    public async Task<IActionResult> GetByScreen(Guid screenId, [FromQuery] DateTime date)
    {
        var showtimes = await _showtimeService.GetByScreenAndDate(screenId, date);
        return Ok(showtimes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShowtime([FromBody] Showtime showtime)
    {
        try
        {
            var created = await _showtimeService.Add(showtime);
            return CreatedAtAction(nameof(GetShowtime), new { id = created.Id }, created);
        }
        catch (ShowtimeConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShowtime(Guid id, [FromBody] Showtime showtime)
    {
        if (id != showtime.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del horario.");
        }

        var existing = await _showtimeService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }

        try
        {
            await _showtimeService.Update(showtime);
            return NoContent();
        }
        catch (ShowtimeConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShowtime(Guid id)
    {
        var existing = await _showtimeService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _showtimeService.Delete(id);
        return NoContent();
    }
}
