using Application.DTOs.Showtimes;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShowtimeController(IShowtimeService showtimeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ShowtimeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShowtimeResponse>>> GetShowtimes(CancellationToken ct) =>
        Ok(await showtimeService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ShowtimeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShowtimeResponse>> GetShowtime(Guid id, CancellationToken ct) =>
        Ok(await showtimeService.GetByIdAsync(id, ct));

    // Todas las funciones de una película, sin importar sala ni fecha.
    [HttpGet("movie/{movieId:guid}")]
    [ProducesResponseType<IReadOnlyList<ShowtimeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShowtimeResponse>>> GetByMovie(Guid movieId, CancellationToken ct) =>
        Ok(await showtimeService.GetByMovieIdAsync(movieId, ct));

    // GET api/showtime/screen/{screenId}?date=2026-08-27
    // La cartelera de una sala en un día puntual (para armar el calendario en el front).
    // BindRequired: sin ?date responde 400 en vez de buscar el 01/01/0001.
    [HttpGet("screen/{screenId:guid}")]
    [ProducesResponseType<IReadOnlyList<ShowtimeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ShowtimeResponse>>> GetByScreen(
        Guid screenId, [FromQuery, BindRequired] DateOnly date, CancellationToken ct) =>
        Ok(await showtimeService.GetByScreenAndDateAsync(screenId, date, ct));

    // El service valida solapamiento antes de guardar; si choca, el handler global responde 409.
    [HttpPost]
    [ProducesResponseType<ShowtimeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShowtimeResponse>> CreateShowtime(CreateShowtimeRequest request, CancellationToken ct)
    {
        var created = await showtimeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetShowtime), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateShowtime(Guid id, UpdateShowtimeRequest request, CancellationToken ct)
    {
        await showtimeService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    // Delete es soft, el service apaga IsActive en vez de borrar la fila.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShowtime(Guid id, CancellationToken ct)
    {
        await showtimeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
