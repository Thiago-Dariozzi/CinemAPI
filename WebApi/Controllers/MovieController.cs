using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MovieController(IMovieService movieService) : ControllerBase
{
    [HttpGet("active")]
    public Task<IReadOnlyList<Movie>> GetActiveMovies(CancellationToken ct)
    {
        return movieService.GetActiveMoviesAsync(ct);
    }

    [HttpGet("genre/{genreId}")]
    public Task<IReadOnlyList<Movie>> GetByGenre(Guid genreId, CancellationToken ct)
    {
        return movieService.GetByGenreAsync(genreId, ct);
    }

    [HttpGet("title/{title}")]
    public Task<IReadOnlyList<Movie>> GetByTitle(string title, CancellationToken ct)
    {
        return movieService.GetByTitleAsync(title, ct);
    }

    [HttpGet]
    public Task<IReadOnlyList<Movie>> GetAll(CancellationToken ct)
    {
        return movieService.GetAllAsync(ct);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await movieService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public Task<Movie> Add([FromBody] Movie movie, CancellationToken ct)
    {
        return movieService.AddAsync(movie, ct);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await movieService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Movie movie, CancellationToken ct)
    {
        if (id != movie.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id de la película.");
        }

        await movieService.UpdateAsync(movie, ct);
        return NoContent();
    }
}
