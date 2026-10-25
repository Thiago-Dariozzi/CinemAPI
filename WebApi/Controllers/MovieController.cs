using Application.DTOs.Movies;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MovieController(IMovieService movieService) : ControllerBase
{
    [HttpGet("active")]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetShowingMovies(CancellationToken ct) =>
        Ok(await movieService.GetShowingMoviesAsync(ct));

    [HttpGet("genre/{genreId:guid}")]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetByGenre(Guid genreId, CancellationToken ct) =>
        Ok(await movieService.GetByGenreAsync(genreId, ct));

    [HttpGet("title/{title}")]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetByTitle(string title, CancellationToken ct) =>
        Ok(await movieService.GetByTitleAsync(title, ct));

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MovieResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetAll(CancellationToken ct) =>
        Ok(await movieService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MovieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> GetById(Guid id, CancellationToken ct) =>
        Ok(await movieService.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<MovieResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovieResponse>> Add(CreateMovieRequest request, CancellationToken ct)
    {
        var created = await movieService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await movieService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateMovieRequest request, CancellationToken ct)
    {
        await movieService.UpdateAsync(id, request, ct);
        return NoContent();
    }
}
