using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    [HttpGet("genre/{genre}")]
    public async Task<IEnumerable<Movie>> GetByGenre(string genre)
    {
        return await _movieService.GetByGenre(genre);
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
    public async Task<Movie?> GetById(Guid id)
    {
        return await _movieService.GetById(id);
    }

    [HttpPost]
    public async Task<Movie?> Add([FromBody] Movie movie)
    {
        return await _movieService.Add(movie);
    }

    [HttpDelete("{id}")]
    public async Task Delete(Guid id)
    {
        await _movieService.Delete(id);   
    }

    [HttpPut]
    public async Task Update([FromBody] Movie movie)
    {
        await _movieService.Update(movie);
    }
}