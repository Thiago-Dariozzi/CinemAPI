using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ScreenController(IScreenService screenService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetScreens(CancellationToken ct)
    {
        return Ok(await screenService.GetAllAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetScreen(Guid id, CancellationToken ct)
    {
        return Ok(await screenService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateScreen([FromBody] Screen screen, CancellationToken ct)
    {
        var created = await screenService.AddAsync(screen, ct);
        return CreatedAtAction(nameof(GetScreen), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScreen(Guid id, [FromBody] Screen screen, CancellationToken ct)
    {
        if (id != screen.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del screen.");
        }

        await screenService.UpdateAsync(screen, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScreen(Guid id, CancellationToken ct)
    {
        await screenService.DeleteAsync(id, ct);
        return NoContent();
    }
}
