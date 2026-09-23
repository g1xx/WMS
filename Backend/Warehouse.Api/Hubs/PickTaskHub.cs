using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Warehouse.Api.Hubs;

// One group per sector, not per user — the picking screen already scopes work
// to a single sector, and every worker in it cares about the same event: a
// task became claimable. Connections are authenticated (see Program.cs's JWT
// query-string handoff for this hub's path); which sectors a connection may
// join isn't restricted further here because sector assignment isn't itself
// a secured resource — the actual pick-task endpoints stay the authority.
[Authorize]
public class PickTaskHub : Hub
{
    public static string GroupName(string sector) => $"sector:{sector}";

    public Task JoinSector(string sector) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sector));

    public Task LeaveSector(string sector) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(sector));
}
