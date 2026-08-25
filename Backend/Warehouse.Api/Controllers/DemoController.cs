using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Common;
using Warehouse.Api.Seeding;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers;

// Feeds the in-app help panel that the review demo shows on every screen. A reviewer
// arrives at a login form with no credentials and, further in, at barcode prompts with
// no idea what a valid barcode looks like — everything here exists to unblock that.
//
// [AllowAnonymous] is required, not an oversight: the first thing the panel has to answer
// is "how do I log in at all". The whole controller is inert unless DemoSettings:Enabled
// is switched on for the deployment (see DemoSettings for why that default matters).
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    // How many live barcodes to show. Enough to pick from if the first one has been used
    // by another reviewer in the meantime, few enough to stay readable on a terminal-sized
    // panel — this is a cheat-sheet, not a data export.
    private const int SampleSize = 8;

    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly DemoSettings _demoSettings;

    public DemoController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser<Guid>> userManager,
        DemoSettings demoSettings)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _demoSettings = demoSettings;
    }

    [HttpGet("help")]
    public async Task<IActionResult> GetHelp()
    {
        // 404 rather than 403: when the demo is off this endpoint should look like it was
        // never deployed, instead of advertising that there's a demo mode to turn on.
        if (!_demoSettings.Enabled)
            return NotFound();

        // Read live, never hardcoded. Containers get consumed by whoever is clicking
        // around the demo, and a hardcoded list goes stale the moment someone claims one.
        var freeContainers = await _unitOfWork.Containers.GetFreeWithLocationAsync();
        var locations = await _unitOfWork.Locations.GetAllOrderedAsync();

        // The supervisor badge barcode is the supervisor's IdentityUser Id (see
        // AuthController.SupervisorOverride), which is generated at seed time and is
        // therefore different in every database — hardcoding it is not even possible.
        var supervisor = await _userManager.FindByNameAsync(DemoDataSeeder.AdminUsername);

        return Ok(new
        {
            Logins = new[]
            {
                new
                {
                    Username = DemoDataSeeder.AdminUsername,
                    Password = DemoDataSeeder.AdminPassword,
                    Role = RoleNames.Admin,
                    Description = "Full access: picking, putaway, stock admin. Also satisfies every "
                        + "Brigadier-or-Admin action, so the same account can act as both the picker "
                        + "and the supervisor who approves an override."
                },
                new
                {
                    Username = DemoDataSeeder.IntegrationUsername,
                    Password = DemoDataSeeder.IntegrationPassword,
                    Role = RoleNames.Integration,
                    Description = "Inbound feed only — creates inbound orders and receiving notices. "
                        + "Cannot adjust stock, dispatch, approve overrides, or register users."
                }
            },

            SupervisorBadge = new
            {
                Barcode = supervisor?.Id.ToString(),
                Description = supervisor == null
                    ? "Unavailable — the demo data has not been seeded in this database."
                    : $"Paste this into the supervisor badge prompt when reporting a missing or "
                        + $"defective item. It is the user id of '{DemoDataSeeder.AdminUsername}', and it "
                        + "differs in every database, so it is read live rather than written down."
            },

            // Available is the only status a container can be claimed from, so these are
            // exactly the barcodes that will work at a "scan container" prompt right now.
            AvailableContainers = freeContainers
                .Select(c => c.Barcode)
                .OrderBy(barcode => barcode)
                .Take(SampleSize)
                .ToList(),

            ConveyorBarcodes = locations
                .Where(l => l.Type == LocationType.ConveyorDrop)
                .Select(l => l.AddressBarcode)
                .OrderBy(barcode => barcode)
                .ToList(),

            ShelfLocations = locations
                .Where(l => l.Type == LocationType.Shelf)
                .Select(l => l.AddressBarcode)
                .OrderBy(barcode => barcode)
                .Take(SampleSize)
                .ToList(),

            // Served from here rather than written into each client: the panel ships in two
            // separate frontend apps, and prose duplicated across both would drift.
            Walkthroughs = new[]
            {
                new
                {
                    Title = "Run a pick task end to end",
                    Steps = new[]
                    {
                        "Log in as admin, then choose Start Picking and enter sector mp1.",
                        "A task is shown with its picking route. Scan one of the Available container barcodes above, then Start Task.",
                        "Enter the location barcode the task asks for, then the product SKU it asks for, then confirm the quantity.",
                        "Once every line is picked the screen switches to dispatch. Enter the same container barcode, then a conveyor barcode from above."
                    }
                },
                new
                {
                    Title = "Do a putaway",
                    Steps = new[]
                    {
                        "Putaway needs a container with inbound work against it — create one from the Inbound Order Feed app, signed in as erp-feed.",
                        "Back in the terminal, choose Start Putaway and enter sector mp1.",
                        "Scan the container barcode from the receiving notice you just created.",
                        "For each line, enter a destination shelf location, then the product SKU. The panel lists valid shelf barcodes, and the screen suggests ranked destinations."
                    }
                },
                new
                {
                    Title = "Report a missing item",
                    Steps = new[]
                    {
                        "Start a pick task and get to the item scan screen.",
                        "Press Escape to open the exceptions menu, then choose 'Item not found'.",
                        "Enter the quantity that is genuinely missing from the shelf.",
                        "At the supervisor badge prompt, paste the supervisor badge barcode from this panel — this is the wall you cannot get past without it.",
                        "The shortfall is written off against the order, and the order finishes as ShortShipped rather than Packed."
                    }
                }
            }
        });
    }
}
