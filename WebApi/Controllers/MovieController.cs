using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MovieController : ControllerBase
{
    private readonly MovieService _movieService;

    public MovieController(MovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet("active")]
    public async Task<IReadOnlyList<Movie>> GetActiveMovies(CancellationToken ct)
    {
        return await _movieService.GetActiveMoviesAsync(ct);
    }

    [HttpGet("genre/{genreId}")]
    public async Task<IReadOnlyList<Movie>> GetByGenre(Guid genreId, CancellationToken ct)
    {
        return await _movieService.GetByGenreAsync(genreId, ct);
    }

    [HttpGet("title/{title}")]
    public async Task<IReadOnlyList<Movie>> GetByTitle(string title, CancellationToken ct)
    {
        return await _movieService.GetByTitleAsync(title, ct);
    }

    [HttpGet]
    public async Task<IReadOnlyList<Movie>> GetAll(CancellationToken ct)
    {
        return await _movieService.GetAllAsync(ct);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var movie = await _movieService.GetByIdAsync(id, ct);
        return movie == null ? NotFound() : Ok(movie);
    }

    [HttpPost]
    public async Task<Movie?> Add([FromBody] Movie movie, CancellationToken ct)
    {
        return await _movieService.AddAsync(movie, ct);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _movieService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _movieService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Movie movie, CancellationToken ct)
    {
        if (id != movie.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id de la película.");
        }

        var existing = await _movieService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _movieService.UpdateAsync(movie, ct);
        return NoContent();
    }
}
