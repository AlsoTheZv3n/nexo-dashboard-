using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;

namespace Dashboard.Tests.Integration;

public class MetricsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
    public async Task PostMetric_WithViewer_Returns403()
    {
        var client = await AuthedAs("viewer", "viewer");
        var response = await client.PostAsJsonAsync("/api/v1/metrics",
            new CreateMetricRequest("cpu.usage", 42.0, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostMetric_WithOperator_Returns201()
    {
        var client = await AuthedAs("operator", "operator");
        var response = await client.PostAsJsonAsync("/api/v1/metrics",
            new CreateMetricRequest($"test.key.{Guid.NewGuid():N}", 3.14, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostMetric_WithInvalidValue_Returns400()
    {
        var client = await AuthedAs("admin", "admin");
        var response = await client.PostAsJsonAsync("/api/v1/metrics",
            new CreateMetricRequest("", 42, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTimeseries_WithPostedData_ReturnsBuckets()
    {
        var admin = await AuthedAs("admin", "admin");
        var key = $"ts.test.{Guid.NewGuid():N}";

        // Seed a few points inside the same hour bucket + one in the previous hour.
        var now = DateTimeOffset.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var twoHoursAgo = now.AddHours(-2);

        foreach (var ts in new[] { now, now.AddMinutes(-15), oneHourAgo, twoHoursAgo })
        {
            await admin.PostAsJsonAsync("/api/v1/metrics",
                new CreateMetricRequest(key, 10, ts, null));
        }

        var from = now.AddHours(-3).ToString("O");
        var to = now.AddMinutes(1).ToString("O");
        var response = await admin.GetAsync(
            $"/api/v1/metrics/timeseries?key={Uri.EscapeDataString(key)}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&bucket=hour&aggregation=count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TimeseriesResponse>();
        payload.Should().NotBeNull();
        payload!.points.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetSummary_Returns200AndScriptCount()
    {
        var client = await AuthedAs("viewer", "viewer");
        var response = await client.GetAsync("/api/v1/metrics/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SummaryResponse>();
        body!.scriptCount.Should().BeGreaterThanOrEqualTo(3); // seed
    }

    [Fact]
    public async Task PostBulk_Over1000Items_Returns400()
    {
        var client = await AuthedAs("admin", "admin");
        var items = Enumerable.Range(0, 1001)
            .Select(i => new CreateMetricRequest("bulk.test", i, null, null))
            .ToList();

        var response = await client.PostAsJsonAsync("/api/v1/metrics/bulk",
            new CreateMetricsBulkRequest(items));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
