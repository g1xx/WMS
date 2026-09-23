using Microsoft.AspNetCore.SignalR;
using Warehouse.Application.Interfaces;

namespace Warehouse.Api.Hubs;

public class SignalRPickTaskNotifier : IPickTaskNotifier
{
    private readonly IHubContext<PickTaskHub> _hubContext;

    public SignalRPickTaskNotifier(IHubContext<PickTaskHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // No payload beyond the event name on purpose: every listener already knows
    // how to refetch its own current task (see PickTasks.tsx), and sending the
    // full task here would mean maintaining a second serialization path that
    // has to stay in sync with the REST DTO.
    public Task NotifySectorChangedAsync(string sector) =>
        _hubContext.Clients.Group(PickTaskHub.GroupName(sector)).SendAsync("PickTasksChanged");
}
