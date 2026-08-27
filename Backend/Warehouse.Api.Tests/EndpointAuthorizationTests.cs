using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Controllers;

namespace Warehouse.Api.Tests;

// Program.cs's FallbackPolicy makes an unattributed endpoint authenticated at runtime, so
// forgetting to think about auth fails closed. These tests pin the same invariant at build
// time, which the fallback alone cannot do: they catch an endpoint that opts OUT with
// [AllowAnonymous], and they catch a controller that quietly relies on the fallback instead
// of stating what it wants.
//
// This exists because ProductsController, LocationsController and ContainersController each
// shipped with zero authorization attributes and served the catalog, the warehouse layout
// and the container fleet to the internet without a token.
public class EndpointAuthorizationTests
{
    // The complete set of endpoints that may be reached without a token. Adding to this
    // list is a security decision, so it should be a deliberate edit to a test that spells
    // out why — not a quiet attribute on a new action.
    private static readonly HashSet<string> ExpectedAnonymousEndpoints = new()
    {
        // Where tokens come from; cannot itself require one.
        "AuthController.Login",

        // The demo help panel, which has to answer "how do I log in at all". Inert unless
        // DemoSettings:Enabled is switched on for the deployment.
        "DemoController.GetHelp",
    };

    private static IEnumerable<(Type Controller, MethodInfo Action)> AllActions()
    {
        return typeof(ProductsController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Select(m => (Controller: t, Action: m)));
    }

    private static bool IsAnonymous(Type controller, MethodInfo action) =>
        action.GetCustomAttribute<AllowAnonymousAttribute>() != null
        || controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;

    private static bool HasExplicitAuthorize(Type controller, MethodInfo action) =>
        action.GetCustomAttribute<AuthorizeAttribute>() != null
        || controller.GetCustomAttribute<AuthorizeAttribute>() != null;

    [Fact]
    public void OnlyTheExpectedEndpointsAreReachableWithoutAToken()
    {
        var anonymous = AllActions()
            .Where(x => IsAnonymous(x.Controller, x.Action))
            .Select(x => $"{x.Controller.Name}.{x.Action.Name}")
            .ToHashSet();

        anonymous.Should().BeEquivalentTo(ExpectedAnonymousEndpoints,
            "every anonymous endpoint is a deliberate hole in the perimeter, so the set must "
            + "be reviewed as a whole rather than grown one attribute at a time");
    }

    [Fact]
    public void EveryOtherEndpointStatesItsOwnAuthorizationRatherThanLeaningOnTheFallback()
    {
        // The fallback policy would already authenticate these, but it authenticates ONLY —
        // it can't express "staff but not the Integration role". A controller that says
        // nothing is a controller nobody decided about, which is how the three anonymous
        // ones happened.
        var silent = AllActions()
            .Where(x => !IsAnonymous(x.Controller, x.Action))
            .Where(x => !HasExplicitAuthorize(x.Controller, x.Action))
            .Select(x => $"{x.Controller.Name}.{x.Action.Name}")
            .ToList();

        silent.Should().BeEmpty(
            "an endpoint with no [Authorize] and no [AllowAnonymous] has had no decision made about it");
    }
}
