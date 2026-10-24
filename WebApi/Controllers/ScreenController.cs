using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ScreenController : ControllerBase
{
    private readonly ScreenService _screenService;

    public ScreenController(ScreenService screenService)
    {
        _screenService = screenService;
    }

    // GET: api/screen
    [HttpGet]
    public async Task<IActionResult> GetScreens(CancellationToken ct)
    {
        var screens = await _screenService.GetAllAsync(ct);
        return Ok(screens);
    }

    // GET: api/screen/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetScreen(Guid id, CancellationToken ct)
    {
        var screen = await _screenService.GetByIdAsync(id, ct);
        if (screen == null)
        {
            return NotFound();
        }
        return Ok(screen);
    }

    // POST: api/screen
    [HttpPost]
    public async Task<IActionResult> CreateScreen([FromBody] Screen screen, CancellationToken ct)
    {
        var created = await _screenService.AddAsync(screen, ct);
        return CreatedAtAction(nameof(GetScreen), new { id = created.Id }, created);
    }

    // PUT: api/screen/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScreen(Guid id, [FromBody] Screen screen, CancellationToken ct)
    {
        if (id != screen.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del screen.");
        }

        var existing = await _screenService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _screenService.UpdateAsync(screen, ct);
        return NoContent();
    }

    // DELETE: api/screen/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScreen(Guid id, CancellationToken ct)
    {
        var existing = await _screenService.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound();
        }

        await _screenService.DeleteAsync(id, ct);
        return NoContent();
    }
}
