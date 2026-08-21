namespace Warehouse.Domain;

public static class RoleNames
{
    public const string Worker = "Worker";
    public const string Brigadier = "Brigadier";
    public const string Admin = "Admin";

    // A non-human identity for an upstream system (ERP/marketplace feed) that pushes
    // inbound orders and receiving notices into the warehouse. Deliberately narrow:
    // it must never be able to touch stock directly, approve overrides, dispatch, or
    // register users — see OrdersController/PutawayTaskController for exactly which
    // actions grant it.
    public const string Integration = "Integration";

    // Compile-time constant (const string concatenation is itself a constant),
    // so it can be used directly in [Authorize(Roles = ...)], which requires one.
    public const string BrigadierOrAdmin = Brigadier + "," + Admin;

    // The human staff roles, as opposed to the non-human Integration role — used to
    // explicitly exclude Integration from actions it has no business calling, rather
    // than relying on it simply not being granted anywhere.
    public const string AnyStaff = Worker + "," + Brigadier + "," + Admin;
}
