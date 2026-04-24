using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;

namespace Dashboard.Tests.Integration;

public class ScriptsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/scripts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithValidToken_ReturnsSeededScripts()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "admin"));
        var tokens = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await client.GetAsync("/api/v1/scripts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ScriptDto>>();
        body.Should().NotBeNull();
        body!.Should().Contain(s => s.Name == "Get-SystemHealth");
        body.Count.Should().BeGreaterThanOrEqualTo(3);
    }
}
