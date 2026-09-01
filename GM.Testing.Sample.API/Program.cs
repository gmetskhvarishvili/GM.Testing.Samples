using GM.Caching;
using GM.Testing.Sample.API;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// A real GM.Caching registration (in-memory here). In an integration test, GM.Testing.Fakes swaps
// this for a FakeCacheService via the GmWebApplicationFactory — no code change in the app.
builder.Services.AddGMCaching();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    message = "GM.Testing sample API — exercised by GmWebApplicationFactory + GM.Testing.Fakes",
    endpoints = RootEndpointDescriptions,
}));

// Cache-backed write/read — the test asserts behaviour AND inspects the fake cache's contents.
app.MapPost("/api/v1/profiles", async (Profile profile, ICacheService cache, CancellationToken ct) =>
{
    await cache.SetAsync($"profile:{profile.Id}", profile, CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5)), ct);
    return Results.Created($"/api/v1/profiles/{profile.Id}", profile);
});

app.MapGet("/api/v1/profiles/{id}", async (string id, ICacheService cache, CancellationToken ct) =>
{
    var profile = await cache.GetAsync<Profile>($"profile:{id}", ct);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

// Reads a configuration value — the test overrides it via GmWebApplicationFactory.WithConfig.
app.MapGet("/api/v1/config/greeting", (IConfiguration config) =>
    Results.Ok(new { greeting = config["Greeting"] ?? "(unset)" }));

// Liveness must not depend on downstream dependencies, so it runs no checks; readiness runs every
// registered health check (none here yet). See engineering baseline §11.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

await app.RunAsync();

// Exposed so the test project can spin the app up with GmWebApplicationFactory.
public partial class Program
{
    // Only used as a WebApplicationFactory<Program> marker; never instantiated directly.
    protected Program() { }

    private static readonly string[] RootEndpointDescriptions =
    [
        "POST /api/v1/profiles {id,name}  → cache the profile under 'profile:{id}'",
        "GET  /api/v1/profiles/{id}       → read it back (404 if absent)",
        "GET  /api/v1/config/greeting     → echoes configuration 'Greeting' (demonstrates WithConfig)",
    ];
}

namespace GM.Testing.Sample.API
{
    /// <summary>A cached user profile — the sample's only domain shape.</summary>
    public sealed record Profile(string Id, string Name);
}
