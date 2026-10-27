using Application.DTOs.Movies;
using Application.DTOs.Showtimes;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Security;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.Staff)]
public sealed class MovieController(IMovieService movieService, IShowtimeService showtimeService) : ControllerBase
{
    // GET api/movie/active?genreId=...&date=2026-11-02 — cartelera; date: películas con funciones ese día.
    [AllowAnonymous]
    [HttpGet("active")]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetShowingMovies(
        [FromQuery] Guid? genreId, [FromQuery] DateOnly? date, CancellationToken ct) =>
        Ok(await movieService.GetShowingMoviesAsync(genreId, date, ct));

    // Funciones futuras de la película agrupadas por día, con butacas libres.
    [AllowAnonymous]
    [HttpGet("{id:guid}/showtimes")]
    [ProducesResponseType<IReadOnlyList<ShowtimeDayResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ShowtimeDayResponse>>> GetSchedule(Guid id, CancellationToken ct) =>
        Ok(await showtimeService.GetScheduleByMovieAsync(id, ct));

    [AllowAnonymous]
    [HttpGet("genre/{genreId:guid}")]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetByGenre(Guid genreId, CancellationToken ct) =>
        Ok(await movieService.GetByGenreAsync(genreId, ct));

    [AllowAnonymous]
    [HttpGet("title/{title}")]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetByTitle(string title, CancellationToken ct) =>
        Ok(await movieService.GetByTitleAsync(title, ct));

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetAll(CancellationToken ct) =>
        Ok(await movieService.GetAllAsync(ct));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MovieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> GetById(Guid id, CancellationToken ct) =>
        Ok(await movieService.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<MovieResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MovieResponse>> Add(CreateMovieRequest request, CancellationToken ct)
    {
        var created = await movieService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await movieService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, UpdateMovieRequest request, CancellationToken ct)
    {
        await movieService.UpdateAsync(id, request, ct);
        return NoContent();
    }
}
