namespace WebApi.Security;

public static class Policies
{
    // Admin y SuperAdmin.
    public const string Staff = "Staff";
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string ClientOnly = "ClientOnly";
}
