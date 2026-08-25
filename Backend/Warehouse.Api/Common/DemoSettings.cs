namespace Warehouse.Api.Common;

// Gates the demo help endpoint (see DemoController). Defaults to DISABLED on purpose:
// that endpoint publishes login credentials, a supervisor badge id, and live container
// and location barcodes without authentication. That is exactly right for the public
// review demo and exactly wrong everywhere else, so switching it on has to be a
// deliberate per-deployment act rather than something that ships by accident.
public class DemoSettings
{
    public bool Enabled { get; set; }
}
