using GM.Caching;

var builder = WebApplication.CreateBuilder(args);

// A real GM.Caching registration (in-memory here). In an integration test, GM.Testing.Fakes swaps
// this for a FakeCacheService via the GmWebApplicationFactory — no code change in the app.
builder.Services.AddGMCaching();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    message = "GM.Testing sample API — exercised by GmWebApplicationFactory + GM.Testing.Fakes",
    endpoints = new[]
    {
        "POST /profiles {id,name}  → cache the profile under 'profile:{id}'",
        "GET  /profiles/{id}       → read it back (404 if absent)",
        "GET  /config/greeting     → echoes configuration 'Greeting' (demonstrates WithConfig)",
    },
}));

// Cache-backed write/read — the test asserts behaviour AND inspects the fake cache's contents.
app.MapPost("/profiles", async (Profile profile, ICacheService cache, CancellationToken ct) =>
{
    await cache.SetAsync($"profile:{profile.Id}", profile, CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5)), ct);
    return Results.Created($"/profiles/{profile.Id}", profile);
});

app.MapGet("/profiles/{id}", async (string id, ICacheService cache, CancellationToken ct) =>
{
    var profile = await cache.GetAsync<Profile>($"profile:{id}", ct);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

// Reads a configuration value — the test overrides it via GmWebApplicationFactory.WithConfig.
app.MapGet("/config/greeting", (IConfiguration config) =>
    Results.Ok(new { greeting = config["Greeting"] ?? "(unset)" }));

app.Run();

public sealed record Profile(string Id, string Name);

// Exposed so the test project can spin the app up with GmWebApplicationFactory.
public partial class Program;
