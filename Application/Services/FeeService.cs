using Application.DTOs.Fees;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// ABM de cargos (solo SuperAdmin). A lo sumo uno vigente: marcar uno desmarca al anterior, y el índice
// único filtrado de Fees.IsCurrent decide si dos pedidos lo hacen al mismo tiempo.
// Cambiar o borrar un cargo no toca las compras hechas: cada una guarda el importe que cobró.
public sealed class FeeService(IFeeRepository feeRepository, IUnitOfWork unitOfWork) : IFeeService
{
    private const string EntityName = "el cargo";

    public async Task<FeeResponse?> GetCurrentAsync(CancellationToken ct) =>
        (await feeRepository.GetCurrentAsync(ct))?.ToResponse();

    // El vigente primero.
    public async Task<IReadOnlyList<FeeResponse>> GetAllAsync(CancellationToken ct) =>
        (await feeRepository.GetAllAsync(ct))
            .OrderByDescending(f => f.IsCurrent)
            .ThenBy(f => f.Name)
            .Select(f => f.ToResponse())
            .ToList();

    public async Task<FeeResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<FeeResponse> CreateAsync(CreateFeeRequest request, CancellationToken ct)
    {
        var fee = request.ToEntity();
        EnsureCents(fee);
        fee.Id = Guid.NewGuid();
        fee.IsActive = true;
        feeRepository.Add(fee);
        await SaveAsync(fee, request.IsCurrent.GetValueOrDefault(), ct);
        return fee.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateFeeRequest request, CancellationToken ct)
    {
        var fee = await GetEntityAsync(id, ct);
        request.ApplyTo(fee);
        EnsureCents(fee);
        await SaveAsync(fee, request.IsCurrent.GetValueOrDefault(), ct);
    }

    // Borrar el vigente deja la API sin cargo hasta que se marque otro.
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var fee = await GetEntityAsync(id, ct);
        fee.IsActive = false;
        fee.IsCurrent = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    // El índice único filtrado se verifica sentencia por sentencia y EF no ordena los UPDATE según él:
    // en un solo SaveChanges podía marcar al nuevo antes de desmarcar al anterior. Por eso se guarda
    // primero la baja del vigente anterior y después el resto, en una transacción.
    private Task SaveAsync(Fee fee, bool isCurrent, CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (isCurrent && !fee.IsCurrent)
            {
                var previous = await feeRepository.GetCurrentForUpdateAsync(token);
                if (previous is not null)
                {
                    previous.IsCurrent = false;
                    await SaveChangesAsync(token);
                }
            }

            fee.IsCurrent = isCurrent;
            await SaveChangesAsync(token);
        }, ct);

    // Las columnas guardan 2 decimales: con más, la base redondearía en silencio.
    private static void EnsureCents(Fee fee)
    {
        if (decimal.Round(fee.Percentage, 2) != fee.Percentage || decimal.Round(fee.FixedAmount, 2) != fee.FixedAmount)
        {
            throw new BusinessRuleException("El porcentaje y el monto fijo admiten hasta 2 decimales.");
        }
    }

    private async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintConflictException)
        {
            throw new FeeConflictException("Otro cargo se marcó como vigente al mismo tiempo. Recargá la lista y volvé a intentar.");
        }
    }

    private async Task<Fee> GetEntityAsync(Guid id, CancellationToken ct) =>
        await feeRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
}
