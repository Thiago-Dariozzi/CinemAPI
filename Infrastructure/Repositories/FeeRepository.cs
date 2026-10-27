using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class FeeRepository(CinemAPIContext context) : BaseRepository<Fee>(context), IFeeRepository
{
    public Task<Fee?> GetCurrentAsync(CancellationToken ct) =>
        Context.Fees.AsNoTracking().FirstOrDefaultAsync(f => f.IsCurrent, ct);
}
