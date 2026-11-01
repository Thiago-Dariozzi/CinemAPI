using Application.DTOs.Screens;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class ScreenService(
    IScreenRepository screenRepository,
    IShowtimeRepository showtimeRepository,
    ITicketRepository ticketRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IScreenService
{
    private const string EntityName = "la sala";

    public async Task<IReadOnlyList<ScreenResponse>> GetAllAsync(CancellationToken ct) =>
        (await screenRepository.GetAllAsync(ct)).Select(s => s.ToResponse()).ToList();

    public async Task<ScreenResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<ScreenResponse> CreateAsync(CreateScreenRequest request, CancellationToken ct)
    {
        var screen = request.ToEntity();
        screen.Id = Guid.NewGuid();
        screen.IsActive = true;
        screenRepository.Add(screen);
        await unitOfWork.SaveChangesAsync(ct);
        return screen.ToResponse();
    }

    // Achicar la sala no puede dejar sin butaca a quien ya compró para una función que no pasó.
    public async Task UpdateAsync(Guid id, UpdateScreenRequest request, CancellationToken ct)
    {
        var screen = await GetEntityAsync(id, ct);
        var shrinks = request.Rows < screen.Rows || request.SeatsPerRow < screen.SeatsPerRow;
        if (shrinks && await ticketRepository.HasSoldSeatsOutsideAsync(id, request.Rows, request.SeatsPerRow, timeProvider.Now(), ct))
        {
            throw new ScreenConflictException(
                "Hay entradas vendidas para funciones próximas en butacas que quedarían fuera de la sala. " +
                "Cancelá esas compras o mantené el tamaño.");
        }

        request.ApplyTo(screen);
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Una función próxima en una sala dada de baja quedaría sin mapa de butacas. Alcanza con mirar las
    // funciones: las que tienen entradas vendidas no se pueden borrar ni mover de sala. Una función que ya
    // empezó no cuenta: sus entradas ya se usaron.
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var screen = await GetEntityAsync(id, ct);
        if (await showtimeRepository.HasUpcomingByScreenAsync(id, timeProvider.Now(), ct))
        {
            throw new ScreenConflictException(
                "La sala tiene funciones próximas: borralas antes de darla de baja (si tienen entradas vendidas, cancelá primero esas compras).");
        }

        screen.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Screen> GetEntityAsync(Guid id, CancellationToken ct) =>
        await screenRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
}
