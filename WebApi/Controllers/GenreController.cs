using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GenreController(IGenreService genreService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGenres(CancellationToken ct)
    {
        return Ok(await genreService.GetAllAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGenre(Guid id, CancellationToken ct)
    {
        return Ok(await genreService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateGenre([FromBody] Genre genre, CancellationToken ct)
    {
        var created = await genreService.AddAsync(genre, ct);
        return CreatedAtAction(nameof(GetGenre), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGenre(Guid id, [FromBody] Genre genre, CancellationToken ct)
    {
        if (id != genre.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del género.");
        }

        await genreService.UpdateAsync(genre, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGenre(Guid id, CancellationToken ct)
    {
        await genreService.DeleteAsync(id, ct);
        return NoContent();
    }
}
