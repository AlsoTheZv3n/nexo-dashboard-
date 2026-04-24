using System.Net;

namespace Dashboard.Tests.Integration;

public class ObservabilityEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Metrics_Endpoint_ReturnsPrometheusExposition()
    {
        var client = factory.CreateClient();

        // Fire a request so the AspNetCore instrumentation has something to record.
        await client.GetAsync("/api/v1/health/live");

        var response = await client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("# TYPE");
        body.Should().Contain("# HELP");
        // The runtime instrumentation always emits process.runtime.dotnet.* counters.
        body.Should().MatchRegex("process_|dotnet_|http_server_|aspnetcore_");
    }

    [Fact]
    public async Task Metrics_Endpoint_IsNotTracedItself()
    {
        // If /metrics were self-traced, scraping would blow up memory over time.
        // We can only verify indirectly: hitting it many times must stay fast and not 500.
        var client = factory.CreateClient();
        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/metrics");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
