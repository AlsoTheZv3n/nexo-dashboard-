using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;

namespace Dashboard.Tests.Integration;

public class ExecutionsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<HttpClient> AuthenticatedClient(string user, string password)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user, password));
        var tokens = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    [Fact]
    public async Task Create_WithAdmin_Returns202()
    {
        var client = await AuthenticatedClient("admin", "admin");

        // Get a seeded script id
        var scripts = (await client.GetFromJsonAsync<List<ScriptDto>>("/api/v1/scripts"))!;
        var target = scripts.First(s => s.Name == "Get-SystemHealth");

        var response = await client.PostAsJsonAsync("/api/v1/executions", new CreateExecutionRequest(
            target.Id,
            new Dictionary<string, object> { ["MinFreeGB"] = 5 }));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<ExecutionDto>();
        body!.ScriptId.Should().Be(target.Id);
        body.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Create_WithViewer_Returns403()
    {
        var client = await AuthenticatedClient("viewer", "viewer");
        var scripts = (await client.GetFromJsonAsync<List<ScriptDto>>("/api/v1/scripts"))!;
        var target = scripts.First();

        var response = await client.PostAsJsonAsync("/api/v1/executions", new CreateExecutionRequest(target.Id, null));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithUnknownScriptId_Returns404()
    {
        var client = await AuthenticatedClient("operator", "operator");
        var response = await client.PostAsJsonAsync(
            "/api/v1/executions",
            new CreateExecutionRequest(Guid.NewGuid(), null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_WithAdmin_Returns200AndHeader()
    {
        var client = await AuthenticatedClient("admin", "admin");
        var response = await client.GetAsync("/api/v1/executions?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().Contain(h => h.Key == "X-Total-Count");
    }
}
