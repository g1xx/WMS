namespace Warehouse.Application.Common;

// Persistence-technology-neutral stand-in for EF Core's DbUpdateConcurrencyException.
// Infrastructure (UnitOfWork) is the only layer allowed to know about EF Core, so it
// catches the real exception there and rethrows this one — services never need an
// EF Core reference just to react to a concurrency conflict (e.g. the xmin token
// on Stock/PickTask/PutawayTask/Order/Container catching two workers racing).
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
