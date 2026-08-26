using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovieController : ControllerBase
{
    private readonly MovieService _movieService;

    public MovieController(MovieService movieService)
    {
        _movieService = movieService; 
    }
    
    [HttpGet("active")]
    public async Task<IEnumerable<Movie>> GetActiveMovies()
    {
        return await _movieService.GetActiveMovies();
    }

    [HttpGet("genre/{genreId}")]
    public async Task<IEnumerable<Movie>> GetByGenre(Guid genreId)
    {
        return await _movieService.GetByGenre(genreId);
    }

    [HttpGet("title/{title}")]
    public async Task<IEnumerable<Movie>> GetByTitle(string title)
    {
        return await _movieService.GetByTitle(title);
    }

    [HttpGet]
    public async Task<IEnumerable<Movie>> GetAll()
    {
        return await _movieService.GetAll();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var movie = await _movieService.GetById(id);
        return movie == null ? NotFound() : Ok(movie);
    }

    [HttpPost]
    public async Task<Movie?> Add([FromBody] Movie movie)
    {
        return await _movieService.Add(movie);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _movieService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }
 
        await _movieService.Delete(id);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Movie movie)
    {
        if (id != movie.Id)
        {
            return BadRequest("El id de la ruta no coincide con el id de la película.");
        }

        var existing = await _movieService.GetById(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _movieService.Update(movie);
        return NoContent();
    }
}