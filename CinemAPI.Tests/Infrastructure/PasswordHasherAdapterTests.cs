using Infrastructure.Security;

namespace CinemAPI.Tests.Infrastructure;

public sealed class PasswordHasherAdapterTests
{
    private readonly PasswordHasherAdapter _sut = new();

    [Fact]
    public void Hash_DoesNotStoreThePasswordAndVerifiesIt()
    {
        var hash = _sut.Hash("Secreta-123");

        Assert.DoesNotContain("Secreta-123", hash, StringComparison.Ordinal);
        Assert.True(_sut.Verify(hash, "Secreta-123"));
        Assert.False(_sut.Verify(hash, "secreta-123"));
    }

    // Salt aleatorio: la misma contraseña nunca da el mismo hash.
    [Fact]
    public void Hash_IsSaltedEachTime()
    {
        Assert.NotEqual(_sut.Hash("Secreta-123"), _sut.Hash("Secreta-123"));
    }

    // Una contraseña vieja en texto plano no es un hash válido: no tira excepción ni la acepta.
    [Fact]
    public void Verify_AgainstAPlainTextValue_ReturnsFalse()
    {
        Assert.False(_sut.Verify("Secreta-123", "Secreta-123"));
    }
}
