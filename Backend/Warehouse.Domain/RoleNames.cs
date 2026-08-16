namespace Warehouse.Domain;

public static class RoleNames
{
    public const string Worker = "Worker";
    public const string Brigadier = "Brigadier";
    public const string Admin = "Admin";

    // Compile-time constant (const string concatenation is itself a constant),
    // so it can be used directly in [Authorize(Roles = ...)], which requires one.
    public const string BrigadierOrAdmin = Brigadier + "," + Admin;
}
