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

    // Todas las funciones de una película, sin importar sala ni fecha.
    [HttpGet("movie/{movieId}")]
    public async Task<IActionResult> GetByMovie(Guid movieId)
    {
        var showtimes = await _showtimeService.GetByMovieId(movieId);
        return Ok(showtimes);
    }

    // GET api/showtime/screen/{screenId}?date=2026-08-27
    // La cartelera de una sala en un día puntual (para armar el calendario en el front).
    [HttpGet("screen/{screenId}")]
    // Si no mandan ?date, el model binding no tira error: date queda en default(DateTime) (01/01/0001).
    public async Task<IActionResult> GetByScreen(Guid screenId, [FromQuery] DateTime date)
    {
        var showtimes = await _showtimeService.GetByScreenAndDate(screenId, date);
        return Ok(showtimes);
    }

    // El service valida solapamiento antes de guardar; si choca, tira Conflict en vez de 500.
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
        // El id de la URL manda; si el body trae otro, es un request mal armado.
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

    // Delete es soft, el service apaga IsActive en vez de borrar la fila.
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
