using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GenreController : ControllerBase
{
    private readonly GenreService _genreService;

    public GenreController(GenreService genreService)
    {
        _genreService = genreService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGenres(CancellationToken ct)
    {
        var genres = await _genreService.GetAllAsync(ct);
        return Ok(genres);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGenre(Guid id, CancellationToken ct)
    {
        var genre = await _genreService.GetByIdAsync(id, ct);
        if (genre == null)
        {
            return NotFound();
        }
        return Ok(genre);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGenre([FromBody] Genre genre, CancellationToken ct)
    {
        try
        {
            var created = await _genreService.AddAsync(genre, ct);
            return CreatedAtAction(nameof(GetGenre), new { id = created.Id }, created);
        }
        catch (GenreConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGenre(Guid id, [FromBody] Genre genre, CancellationToken ct)
    {
        if (id != genre.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del género.");
        }

        var existing = await _genreService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        try
        {
            await _genreService.UpdateAsync(genre, ct);
            return NoContent();
        }
        catch (GenreConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGenre(Guid id, CancellationToken ct)
    {
        var existing = await _genreService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        try
        {
            await _genreService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (GenreConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
