namespace Domain.Entities;

/// <summary>
/// Roles del sistema. Se guardan como texto en Users.Role y viajan en el claim "role" del JWT.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Client = "Client";

    public static bool IsStaff(string role) => role is SuperAdmin or Admin;
}
