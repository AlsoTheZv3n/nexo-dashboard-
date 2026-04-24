using System.Net;

namespace Dashboard.Tests.Integration;

public class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Live_Returns200()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_Returns200_WhenDbReachable()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
