using System.Security.Claims;
using Application.Interfaces;

namespace WebApi.Security;

// Lee el usuario de los claims del token ya validado. Solo se usa desde endpoints con [Authorize]:
// pedir el Id fuera de una request autenticada es un error de programación, no del cliente.
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid Id => Guid.TryParse(Principal.FindFirstValue(ClaimNames.Subject), out var id)
        ? id
        : throw new InvalidOperationException("La request no está autenticada.");

    public string Role => Principal.FindFirstValue(ClaimNames.Role) ?? string.Empty;

    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No hay una request en curso.");
}
