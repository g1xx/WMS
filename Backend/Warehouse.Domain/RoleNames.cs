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
    //
    // BEFORE CHANGING ANY ROLE GATE, CHECK BOTH FRONTENDS. This repo has two client apps
    // signing in as two different identities:
    //
    //   Frontend/warehouse-client    — staff terminal, signs in as Worker/Brigadier/Admin
    //   Frontend/TestOrderGenerator  — "Inbound Order Feed", signs in as erp-feed
    //                                  (RoleNames.Integration)
    //
    // Putting AnyStaff on an endpoint therefore does not merely "require a login" — it
    // locks the feed out, because Integration is deliberately not in this list. That is
    // exactly how GET /api/Products broke: its callers were checked in warehouse-client,
    // the feed was not, and the feed's product picker went 403 in production.
    //
    // The automated tests cannot catch this. EndpointAuthorizationTests verifies that every
    // endpoint HAS an authorization decision and pins which roles reach which endpoint, but
    // it cannot know which client calls what. The e2e suites can't catch it either — they
    // stub the API with page.route, so a mocked call returns 200 no matter what the real
    // gate says. Checking the callers is a manual step, and this comment is the reminder.
    public const string AnyStaff = Worker + "," + Brigadier + "," + Admin;
}
