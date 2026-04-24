using System.Net;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;

namespace Dashboard.Tests.Integration;

public class AuthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Login_WithSeededAdmin_Returns200AndTokens()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "admin"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.User.Username.Should().Be("admin");
        body.User.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "wrong"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyBody_Returns400()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("", ""));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_Returns200()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "admin"));
        var tokens = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(tokens.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
