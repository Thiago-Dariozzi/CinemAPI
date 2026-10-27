using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CinemAPI.Tests.Infrastructure;

// Base temporal en LocalDB (SQL Server real, no el proveedor InMemory: hace falta la collation,
// los índices filtrados y la traducción real de las consultas). Aplica las migraciones, así que
// también verifica que se puedan aplicar desde cero. Se borra al terminar.
public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        $@"Server=(localdb)\MSSQLLocalDB;Database=CinemAPI_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;";

    public CinemAPIContext CreateContext() =>
        new(new DbContextOptionsBuilder<CinemAPIContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerTestGroup : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SqlServer";
}
