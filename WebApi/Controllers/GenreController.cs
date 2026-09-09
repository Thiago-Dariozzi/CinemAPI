using System;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenreController : ControllerBase
{
    private readonly GenreService _genreService;

    public GenreController(GenreService genreService)
    {
        _genreService = genreService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGenres()
    {
        var genres = await _genreService.GetAll();
        return Ok(genres);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGenre(Guid id)
    {
        var genre = await _genreService.GetById(id);
        if (genre == null)
        {
            return NotFound();
        }
        return Ok(genre);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGenre([FromBody] Genre genre)
    {
        try
        {
            var created = await _genreService.Add(genre);
            return CreatedAtAction(nameof(GetGenre), new { id = created.Id }, created);
        }
        catch (GenreConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGenre(Guid id, [FromBody] Genre genre)
    {
        if (id != genre.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id del género.");
        }

        var existing = await _genreService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }

        try
        {
            await _genreService.Update(genre);
            return NoContent();
        }
        catch (GenreConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGenre(Guid id)
    {
        var existing = await _genreService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }

        try
        {
            await _genreService.Delete(id);
            return NoContent();
        }
        catch (GenreConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
