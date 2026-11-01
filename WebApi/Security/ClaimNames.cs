namespace WebApi.Security;

// Nombres cortos de los claims del token. MapInboundClaims = false hace que lleguen tal cual, sin
// traducirlos a las URIs de ClaimTypes.
public static class ClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}
