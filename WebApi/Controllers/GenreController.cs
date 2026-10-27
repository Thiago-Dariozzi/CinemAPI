using Application.DTOs.Genres;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Security;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.Staff)]
public sealed class GenreController(IGenreService genreService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GenreResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GenreResponse>>> GetGenres(CancellationToken ct) =>
        Ok(await genreService.GetAllAsync(ct));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GenreResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenreResponse>> GetGenre(Guid id, CancellationToken ct) =>
        Ok(await genreService.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<GenreResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GenreResponse>> CreateGenre(CreateGenreRequest request, CancellationToken ct)
    {
        var created = await genreService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetGenre), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGenre(Guid id, UpdateGenreRequest request, CancellationToken ct)
    {
        await genreService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteGenre(Guid id, CancellationToken ct)
    {
        await genreService.DeleteAsync(id, ct);
        return NoContent();
    }
}
