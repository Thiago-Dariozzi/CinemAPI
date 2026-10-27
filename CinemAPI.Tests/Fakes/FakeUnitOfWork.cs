using Application.Interfaces;

namespace CinemAPI.Tests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    // Permite simular lo que pasa en la base al guardar (por ejemplo, otro pedido que ganó la carrera).
    public Action? OnSave { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        OnSave?.Invoke();
        return Task.FromResult(0);
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) =>
        work(cancellationToken);
}
