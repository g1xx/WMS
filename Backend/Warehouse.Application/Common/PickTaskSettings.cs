namespace Warehouse.Application.Common;

// Plain POCO on purpose — Warehouse.Application has no package references at all (only
// Warehouse.Domain), so there's no IOptions<T> here. The Api project binds this from
// configuration and registers the instance; see Program.cs.
public class PickTaskSettings
{
    // How long a task may sit claimed-but-not-started before the sweep hands it back to
    // the queue. "Not started" means the worker was shown the task but never scanned a
    // container — once scanned the task is InProgress and the sweep cannot reach it, so
    // this timeout never interrupts a picker who is genuinely working the racks.
    public int ClaimTimeoutMinutes { get; set; } = 15;
}
