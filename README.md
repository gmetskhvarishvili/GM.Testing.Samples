# GM.Testing.Samples

Runnable usage for [GM.Testing](https://github.com/gmetskhvarishvili/GM.Testing) — the shared testing
utilities for the `GM.*` ecosystem.

`GM.Testing.Sample.API` is a tiny GM.Caching-backed minimal API (a cache-backed profile store). It
depends only on **real** GM.* packages — never on GM.Testing — which is the point: the app doesn't know
it's being tested.

`GM.Testing.Sample.Tests` exercises it through the whole kit:

```csharp
using var factory = new GmWebApplicationFactory<Program>()
    .WithServices(s => s.AddFakeCache());        // swap the real cache for the in-memory fake
var client = factory.CreateClient();

var created = await (await client.PostAsJsonAsync("/api/v1/profiles", new Profile("u1", "Ada")))
    .ShouldBeCreatedAsync<Profile>();            // HTTP helper: 201 + body in one call

// inspect the fake the app actually wrote through
var cache = factory.Services.GetRequiredService<FakeCacheService>();
Assert.Contains("profile:u1", cache.Keys);
```

The tests cover: the `GmWebApplicationFactory` base, DI overrides (real cache → fake), config
overrides (`.WithConfig`), the HTTP assertion helpers, inspecting a fake to assert a side effect, and
the `/health/live` + `/health/ready` endpoints.

```bash
dotnet test
```

> The test project references the published `GM.Testing.*` packages by `PackageReference`. For local
> development against the sibling source repo, use the `ProjectReference` form on the
> `feat/testing-sample` branch instead.
