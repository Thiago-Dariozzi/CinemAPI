using Application.DTOs.Screens;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ScreenController(IScreenService screenService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ScreenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScreenResponse>>> GetScreens(CancellationToken ct) =>
        Ok(await screenService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ScreenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScreenResponse>> GetScreen(Guid id, CancellationToken ct) =>
        Ok(await screenService.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<ScreenResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScreenResponse>> CreateScreen(CreateScreenRequest request, CancellationToken ct)
    {
        var created = await screenService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetScreen), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScreen(Guid id, UpdateScreenRequest request, CancellationToken ct)
    {
        await screenService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScreen(Guid id, CancellationToken ct)
    {
        await screenService.DeleteAsync(id, ct);
        return NoContent();
    }
}
