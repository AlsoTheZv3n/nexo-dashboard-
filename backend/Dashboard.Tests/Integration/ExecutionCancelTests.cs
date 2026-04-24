using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;

namespace Dashboard.Tests.Integration;

public class ExecutionCancelTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<HttpClient> AuthedAs(string user, string password)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user, password));
        var body = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return client;
    }

    [Fact]
    public async Task Cancel_OnUnknownExecution_Returns404()
    {
        var client = await AuthedAs("admin", "admin");
        var response = await client.PostAsync($"/api/v1/executions/{Guid.NewGuid()}/cancel", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_WithViewer_Returns403()
    {
        var client = await AuthedAs("viewer", "viewer");
        var response = await client.PostAsync($"/api/v1/executions/{Guid.NewGuid()}/cancel", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
