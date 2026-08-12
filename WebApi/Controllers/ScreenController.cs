using System;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Entities;
 
namespace Controllers.ScreenController;
 
[ApiController]
[Route("api/[controller]")]
public class ScreenController : ControllerBase
{
    private readonly ScreenService _screenService;
 
    public ScreenController(ScreenService screenService)
    {
        _screenService = screenService;
    }
 
    // GET: api/screen
    [HttpGet]
    public async Task<IActionResult> GetScreens()
    {
        var screens = await _screenService.GetAll();
        return Ok(screens);
    }
 
    // GET: api/screen/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetScreen(Guid id)
    {
        var screen = await _screenService.GetById(id);
        if (screen == null)
        {
            return NotFound();
        }
        return Ok(screen);
    }
 
    // POST: api/screen
    [HttpPost]
    public async Task<IActionResult> CreateScreen([FromBody] Screen screen)
    {
        var created = await _screenService.Add(screen);
        return CreatedAtAction(nameof(GetScreen), new { id = created.Id }, created);
    }
 
    // PUT: api/screen/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScreen(Guid id, [FromBody] Screen screen)
    {
        if (id != screen.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del screen.");
        }
 
        var existing = await _screenService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }
 
        await _screenService.Update(screen);
        return NoContent();
    }
 
    // DELETE: api/screen/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScreen(Guid id)
    {
        var existing = await _screenService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }
 
        await _screenService.Delete(id);
        return NoContent();
    }
}
 