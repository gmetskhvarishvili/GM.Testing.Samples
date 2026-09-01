using System.Net.Http.Json;
using GM.Testing;                 // HTTP assertion helpers
using GM.Testing.AspNetCore;      // GmWebApplicationFactory
using GM.Testing.Fakes;           // FakeCacheService + AddFakeCache
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GM.Testing.Sample.Tests;

// End-to-end demonstration of the GM.Testing kit against the sample API: the WebApplicationFactory
// base, DI overrides (swap real cache for the fake), config overrides, HTTP assertion helpers, and
// inspecting the fake to assert side effects.
public class SampleIntegrationTests
{
    private sealed record Profile(string Id, string Name);

    [Fact]
    public async Task Post_then_get_profile_round_trips_and_hits_the_fake_cache()
    {
        using var factory = new GmWebApplicationFactory<Program>()
            .WithServices(s => s.AddFakeCache());
        var client = factory.CreateClient();

        // HTTP helpers: status + body in one call.
        var postResponse = await client.PostAsJsonAsync("/api/v1/profiles", new Profile("u1", "Ada"));
        var created = await postResponse.ShouldBeCreatedAsync<Profile>();
        Assert.Equal("Ada", created.Name);

        var fetched = await (await client.GetAsync("/api/v1/profiles/u1")).ShouldBeOkAsync<Profile>();
        Assert.Equal(new Profile("u1", "Ada"), fetched);

        // Inspect the fake the app actually wrote through — same singleton instance.
        var cache = factory.Services.GetRequiredService<FakeCacheService>();
        Assert.Contains("profile:u1", cache.Keys);
    }

    [Fact]
    public async Task Missing_profile_returns_404()
    {
        using var factory = new GmWebApplicationFactory<Program>().WithServices(s => s.AddFakeCache());
        var client = factory.CreateClient();

        await (await client.GetAsync("/api/v1/profiles/absent")).ShouldBeNotFoundAsync();
    }

    [Fact]
    public async Task Config_override_flows_into_the_app()
    {
        using var factory = new GmWebApplicationFactory<Program>()
            .WithConfig("Greeting", "hello-from-test");
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/config/greeting");
        var body = await response.ShouldBeOkAsync<GreetingDto>();
        Assert.Equal("hello-from-test", body.Greeting);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_report_healthy(string path)
    {
        using var factory = new GmWebApplicationFactory<Program>();
        var client = factory.CreateClient();

        await (await client.GetAsync(path)).ShouldBeOkAsync();
    }

    private sealed record GreetingDto(string Greeting);
}
