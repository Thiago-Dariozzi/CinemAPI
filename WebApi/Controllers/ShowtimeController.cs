using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShowtimeController(IShowtimeService showtimeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetShowtimes(CancellationToken ct)
    {
        return Ok(await showtimeService.GetAllAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShowtime(Guid id, CancellationToken ct)
    {
        return Ok(await showtimeService.GetByIdAsync(id, ct));
    }

    // Todas las funciones de una película, sin importar sala ni fecha.
    [HttpGet("movie/{movieId}")]
    public async Task<IActionResult> GetByMovie(Guid movieId, CancellationToken ct)
    {
        return Ok(await showtimeService.GetByMovieIdAsync(movieId, ct));
    }

    // GET api/showtime/screen/{screenId}?date=2026-08-27
    // La cartelera de una sala en un día puntual (para armar el calendario en el front).
    [HttpGet("screen/{screenId}")]
    // Si no mandan ?date, el model binding no tira error: date queda en default(DateTime) (01/01/0001).
    public async Task<IActionResult> GetByScreen(Guid screenId, [FromQuery] DateTime date, CancellationToken ct)
    {
        return Ok(await showtimeService.GetByScreenAndDateAsync(screenId, date, ct));
    }

    // El service valida solapamiento antes de guardar; si choca, el handler global responde 409.
    [HttpPost]
    public async Task<IActionResult> CreateShowtime([FromBody] Showtime showtime, CancellationToken ct)
    {
        var created = await showtimeService.AddAsync(showtime, ct);
        return CreatedAtAction(nameof(GetShowtime), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShowtime(Guid id, [FromBody] Showtime showtime, CancellationToken ct)
    {
        // El id de la URL manda; si el body trae otro, es un request mal armado.
        if (id != showtime.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del horario.");
        }

        await showtimeService.UpdateAsync(showtime, ct);
        return NoContent();
    }

    // Delete es soft, el service apaga IsActive en vez de borrar la fila.
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShowtime(Guid id, CancellationToken ct)
    {
        await showtimeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
