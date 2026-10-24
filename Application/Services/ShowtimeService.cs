using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// Orquesta IShowtimeRepository + IMovieRepository (necesito la duración de la
// película para calcular solapamientos). La llama ShowtimeController.
public sealed class ShowtimeService(
    IShowtimeRepository showtimeRepository,
    IMovieRepository movieRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<IEnumerable<Showtime>> GetAll()
    {
        return await showtimeRepository.GetAll();
    }

    public async Task<Showtime?> GetById(Guid id)
    {
        return await showtimeRepository.GetById(id);
    }

    public async Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId)
    {
        return await showtimeRepository.GetByMovieId(movieId);
    }

    // Cartelera de una sala en un día (la usa el calendario del front).
    public async Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date)
    {
        return await showtimeRepository.GetByScreenAndDate(screenId, date);
    }

    // Id lo generamos acá, no lo mandamos a confiar del cliente.
    public async Task<Showtime> Add(Showtime showtime)
    {
        showtime.Id = Guid.NewGuid();
        showtime.IsActive = true;
        await ValidateNoOverlap(showtime);

        showtimeRepository.Add(showtime);
        await unitOfWork.SaveChangesAsync();
        return showtime;
    }

    // Valida con los datos nuevos antes de tocar la entidad trackeada (ValidateNoOverlap
    // excluye el propio Id, así que no choca contra sí misma).
    public async Task Update(Showtime showtime)
    {
        var existing = await showtimeRepository.GetById(showtime.Id);
        if (existing == null)
        {
            return;
        }

        await ValidateNoOverlap(showtime);

        existing.MovieId = showtime.MovieId;
        existing.ScreenId = showtime.ScreenId;
        existing.StartTime = showtime.StartTime;
        existing.Price = showtime.Price;
        existing.IsActive = showtime.IsActive;
        await unitOfWork.SaveChangesAsync();
    }

    // Soft delete: apaga IsActive en vez de borrar la fila.
    public async Task Delete(Guid id)
    {
        var showtime = await showtimeRepository.GetById(id);
        if (showtime != null)
        {
            showtime.IsActive = false;
            await unitOfWork.SaveChangesAsync();
        }
    }

    // El tramo que ocupa una función es [StartTime, StartTime + duración de la película
    // + CleaningBufferMinutes]; dos tramos chocan si se cruzan aunque sea
    // parcialmente (tocarse justo en el borde no cuenta como choque). Solo compara
    // funciones del mismo día calendario que StartTime.
    private async Task ValidateNoOverlap(Showtime showtime)
    {
        var movie = await movieRepository.GetById(showtime.MovieId)
            ?? throw new ShowtimeConflictException($"No se encontró la película con Id {showtime.MovieId}.");

        var newStart = showtime.StartTime;
        var newEnd = newStart.AddMinutes(movie.DurationMinutes + Showtime.CleaningBufferMinutes);

        // Una sola consulta trae las funciones de esa sala en ese día con la duración de su película.
        var occupied = await showtimeRepository.GetOccupancyByScreenAndDate(showtime.ScreenId, newStart);

        foreach (var other in occupied.Where(o => o.ShowtimeId != showtime.Id))
        {
            var otherEnd = other.StartTime.AddMinutes((other.DurationMinutes ?? 0) + Showtime.CleaningBufferMinutes);

            // Choque clásico de intervalos: se pisan si cada uno empieza antes de que
            // termine el otro. Con < (no <=) tocarse justo en el borde no cuenta.
            var overlaps = newStart < otherEnd && other.StartTime < newEnd;
            if (overlaps)
            {
                throw new ShowtimeConflictException(
                    $"La sala ya tiene programada la función '{other.MovieTitle ?? "otra película"}' " +
                    $"(Showtime Id {other.ShowtimeId}) de {other.StartTime:HH:mm} a {otherEnd:HH:mm} el " +
                    $"{other.StartTime:dd/MM/yyyy}; esta función se superpone con esa franja.");
            }
        }
    }
}
