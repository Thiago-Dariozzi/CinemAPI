using System;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class ShowtimeService
{
    private readonly IShowtimeRepository _repo;

    public ShowtimeService(IShowtimeRepository showtimeRepository)
    {
        _repo = showtimeRepository;
    }

    public async Task<IEnumerable<Showtime>> GetAll()
    {
        return await _repo.GetAll();
    }

    public async Task<Showtime?> GetById(Guid id)
    {
        return await _repo.GetById(id);
    }

    public async Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId)
    {
        return await _repo.GetByMovieId(movieId);
    }

    public async Task<Showtime> Add(Showtime showtime)
    {
        showtime.Id = Guid.NewGuid();
        showtime.IsActive = true;
        return await _repo.Add(showtime);
    }

    public async Task Update(Showtime showtime)
    {
        await _repo.Update(showtime);
    }

    public async Task Delete(Guid id)
    {
        var showtime = await _repo.GetById(id);
        if (showtime != null)
        {
            showtime.IsActive = false;
            await _repo.Update(showtime);
        }
    }
}
