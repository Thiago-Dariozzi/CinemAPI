using Application.DTOs.Purchases;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Models;

namespace Application.Services;

// Compra y cancelación de entradas. El cliente compra para sí mismo (el usuario sale del token) y solo
// ve y cancela lo suyo; el staff ve todas las ventas y cancela cualquiera, sin límite de tiempo.
public sealed class PurchaseService(
    IPurchaseRepository purchaseRepository,
    IShowtimeRepository showtimeRepository,
    IMovieRepository movieRepository,
    IScreenRepository screenRepository,
    ITicketRepository ticketRepository,
    IFeeRepository feeRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IPurchaseService
{
    public const int MaxSeatsPerPurchase = 10;

    // El cliente puede cancelar mientras falten MÁS de 2 horas para la función.
    public static readonly TimeSpan ClientCancellationNotice = TimeSpan.FromHours(2);

    private const string EntityName = "la compra";

    private bool IsStaff => Roles.IsStaff(currentUser.Role);

    public async Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request, CancellationToken ct)
    {
        // [Required] no valida los elementos: "seats":[null] llega hasta acá.
        if (request.Seats?.Any(s => s is null) == true)
        {
            throw new BusinessRuleException("Cada butaca tiene que indicar fila y número.");
        }

        var seats = (request.Seats ?? []).Select(s => new Seat(s!.Row, s.Number)).ToList();
        EnsureSeatCount(seats);

        var showtimeId = request.ShowtimeId.GetValueOrDefault();
        var showtime = await showtimeRepository.GetByIdAsync(showtimeId, ct)
            ?? throw new InvalidReferenceException("la función", showtimeId);
        var now = timeProvider.Now();
        if (showtime.StartTime <= now)
        {
            throw new BusinessRuleException("La función ya empezó: no se pueden comprar entradas.");
        }

        var movie = await movieRepository.GetByIdAsync(showtime.MovieId, ct);
        if (movie is not { IsShowing: true })
        {
            throw new BusinessRuleException("La película de esta función no está en cartelera.");
        }

        var screen = await screenRepository.GetByIdAsync(showtime.ScreenId, ct)
            ?? throw new BusinessRuleException("La sala de esta función no está disponible.");
        EnsureSeatsExist(seats, screen);
        await EnsureSeatsAreFreeAsync(showtime.Id, seats, ct);

        var purchase = await BuildPurchaseAsync(showtime, seats, now, ct);
        purchaseRepository.Add(purchase);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintConflictException ex)
        {
            // Otra compra tomó alguna de estas butacas entre la validación y el guardado.
            throw new SeatConflictException("Alguna de las butacas elegidas se acaba de vender. Actualizá el mapa y elegí otras.", ex);
        }

        return await GetResponseAsync(purchase.Id, now, ct);
    }

    public async Task<IReadOnlyList<PurchaseResponse>> GetMineAsync(CancellationToken ct)
    {
        var now = timeProvider.Now();
        return (await purchaseRepository.GetByUserAsync(currentUser.Id, ct)).Select(p => ToResponse(p, now)).ToList();
    }

    public async Task<PurchaseResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var purchase = await purchaseRepository.GetDetailsAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
        EnsureCanAccess(purchase.UserId, "Solo podés ver tus propias compras.");
        return ToResponse(purchase, timeProvider.Now());
    }

    public async Task<IReadOnlyList<PurchaseResponse>> SearchAsync(PurchaseSearchRequest request, CancellationToken ct)
    {
        if (request.Status is not (null or PurchaseStatus.Confirmed or PurchaseStatus.Cancelled))
        {
            throw new BusinessRuleException($"El estado '{request.Status}' no existe. Usá {PurchaseStatus.Confirmed} o {PurchaseStatus.Cancelled}.");
        }

        if (request.From > request.To)
        {
            throw new BusinessRuleException("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");
        }

        // Días completos: [From 00:00, To + 1 día 00:00).
        var filter = new PurchaseFilter(
            request.MovieId,
            request.ShowtimeId,
            request.From?.ToDateTime(TimeOnly.MinValue),
            request.To is { } to ? DateRules.EnsureHasNextDay(to).AddDays(1).ToDateTime(TimeOnly.MinValue) : null,
            request.Status);
        var now = timeProvider.Now();
        return (await purchaseRepository.SearchAsync(filter, ct)).Select(p => ToResponse(p, now)).ToList();
    }

    // El cliente cancela solo lo suyo y con el límite de 2 h.
    public async Task<PurchaseResponse> CancelMineAsync(Guid id, CancellationToken ct)
    {
        var purchase = await GetPurchaseAsync(id, ct);
        if (purchase.UserId != currentUser.Id)
        {
            throw new ForbiddenException("Solo podés cancelar tus propias compras.");
        }

        EnsureNotCancelled(purchase);

        var now = timeProvider.Now();
        var showtime = await showtimeRepository.GetByIdAsync(purchase.ShowtimeId, ct);
        if (showtime is null || !IsBeforeClientDeadline(showtime.StartTime, now))
        {
            throw new BusinessRuleException("Las compras se pueden cancelar hasta 2 horas antes de la función.");
        }

        return await CancelAsync(purchase, now, ct);
    }

    // El staff cancela cualquier compra, sin límite de tiempo. La política del endpoint ya lo exige;
    // el servicio lo vuelve a verificar para no depender solo de la ruta.
    public async Task<PurchaseResponse> CancelAnyAsync(Guid id, CancellationToken ct)
    {
        if (!IsStaff)
        {
            throw new ForbiddenException("Solo el staff puede cancelar compras de otros clientes.");
        }

        var purchase = await GetPurchaseAsync(id, ct);
        EnsureNotCancelled(purchase);
        return await CancelAsync(purchase, timeProvider.Now(), ct);
    }

    private async Task<Purchase> GetPurchaseAsync(Guid id, CancellationToken ct) =>
        await purchaseRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    // Va después de verificar el dueño: a un cliente, el 409 de una compra ajena le revelaría que existe.
    private static void EnsureNotCancelled(Purchase purchase)
    {
        if (purchase.IsCancelled)
        {
            throw new PurchaseConflictException("La compra ya estaba cancelada.");
        }
    }

    private async Task<PurchaseResponse> CancelAsync(Purchase purchase, DateTime now, CancellationToken ct)
    {
        purchase.Status = PurchaseStatus.Cancelled;
        purchase.CancelledAt = now;
        foreach (var ticket in purchase.Tickets)
        {
            ticket.IsCancelled = true;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await GetResponseAsync(purchase.Id, now, ct);
    }

    private static void EnsureSeatCount(List<Seat> seats)
    {
        if (seats.Count is 0 or > MaxSeatsPerPurchase)
        {
            throw new BusinessRuleException($"Elegí entre 1 y {MaxSeatsPerPurchase} butacas por compra.");
        }

        var repeated = seats.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (repeated.Count > 0)
        {
            throw new BusinessRuleException(repeated.Count == 1
                ? $"Elegiste más de una vez la butaca {SeatLabels.Join(repeated)}."
                : $"Elegiste más de una vez las butacas {SeatLabels.Join(repeated)}.");
        }
    }

    private static void EnsureSeatsExist(List<Seat> seats, Screen screen)
    {
        var missing = seats.Where(s => !screen.HasSeat(s.Row, s.Number)).ToList();
        if (missing.Count > 0)
        {
            throw new BusinessRuleException(
                $"La sala \"{screen.Name}\" no tiene la butaca {SeatLabels.Join(missing)} " +
                $"(filas A a {(char)('A' + screen.Rows - 1)}, butacas 1 a {screen.SeatsPerRow}).");
        }
    }

    private async Task EnsureSeatsAreFreeAsync(Guid showtimeId, List<Seat> seats, CancellationToken ct)
    {
        var taken = (await ticketRepository.GetTakenSeatsAsync(showtimeId, ct)).ToHashSet();
        var conflicts = seats.Where(taken.Contains).ToList();
        if (conflicts.Count > 0)
        {
            throw new SeatConflictException(conflicts.Count == 1
                ? $"La butaca {SeatLabels.Join(conflicts)} ya está vendida. Elegí otra."
                : $"Las butacas {SeatLabels.Join(conflicts)} ya están vendidas. Elegí otras.");
        }
    }

    // Los importes quedan guardados: si después cambia el precio de la función o el cargo vigente,
    // la compra no se recalcula.
    private async Task<Purchase> BuildPurchaseAsync(Showtime showtime, List<Seat> seats, DateTime now, CancellationToken ct)
    {
        var fee = await feeRepository.GetCurrentAsync(ct);
        var subtotal = showtime.Price * seats.Count;
        var feeAmount = fee?.CalculateFor(subtotal) ?? 0m;
        var purchaseId = Guid.NewGuid();

        return new Purchase
        {
            Id = purchaseId,
            UserId = currentUser.Id,
            ShowtimeId = showtime.Id,
            CreatedAt = now,
            Subtotal = subtotal,
            FeeId = fee?.Id,
            FeeAmount = feeAmount,
            Total = subtotal + feeAmount,
            Status = PurchaseStatus.Confirmed,
            IsActive = true,
            Tickets = seats.Select(s => new Ticket
            {
                Id = Guid.NewGuid(),
                PurchaseId = purchaseId,
                ShowtimeId = showtime.Id,
                SeatRow = s.Row,
                SeatNumber = s.Number,
                Price = showtime.Price,
                IsActive = true,
            }).ToList(),
        };
    }

    private void EnsureCanAccess(Guid ownerId, string message)
    {
        if (!IsStaff && ownerId != currentUser.Id)
        {
            throw new ForbiddenException(message);
        }
    }

    private async Task<PurchaseResponse> GetResponseAsync(Guid id, DateTime now, CancellationToken ct)
    {
        var purchase = await purchaseRepository.GetDetailsAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
        return ToResponse(purchase, now);
    }

    private PurchaseResponse ToResponse(PurchaseDetails purchase, DateTime now) =>
        purchase.ToResponse(canCancel: purchase.Status == PurchaseStatus.Confirmed
            && (IsStaff || IsBeforeClientDeadline(purchase.StartTime, now)));

    private static bool IsBeforeClientDeadline(DateTime startTime, DateTime now) =>
        startTime - now > ClientCancellationNotice;
}
