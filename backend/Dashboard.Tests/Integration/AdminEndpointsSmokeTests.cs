using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;
using Dashboard.Core.Entities;

namespace Dashboard.Tests.Integration;

/// <summary>
/// Smoke tests for the admin-only surface added in the "operational platform" batch:
/// users, audit, api-keys, alerts, schedules. Covers RBAC basics + one happy path
/// per controller. Exhaustive semantics are pushed into their own test classes
/// when they accumulate (keeps this file short).
/// </summary>
public class AdminEndpointsSmokeTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
    public async Task Users_GetAll_RequiresAdmin()
    {
        var op = await AuthedAs("operator", "operator");
        (await op.GetAsync("/api/v1/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var admin = await AuthedAs("admin", "admin");
        var response = await admin.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        body!.Should().Contain(u => u.username == "admin");
    }

    [Fact]
    public async Task Users_Create_ReturnsSeededUserInList()
    {
        var admin = await AuthedAs("admin", "admin");
        var username = $"smoke-{Guid.NewGuid():N}"[..20];

        var created = await admin.PostAsJsonAsync("/api/v1/users",
            new CreateUserRequest(username, "supersecret", UserRole.Operator));
        var body = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, $"but body was: {body}");

        var login = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(username, "supersecret"));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiKey_Created_AuthenticatesInstead_OfJwt()
    {
        var admin = await AuthedAs("admin", "admin");
        var created = await admin.PostAsJsonAsync("/api/v1/api-keys",
            new CreateApiKeyRequest("smoke", UserRole.Viewer, null));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await created.Content.ReadFromJsonAsync<ApiKeyCreatedDto>();
        payload!.plaintext.Should().StartWith("nxk_");

        // Use the plaintext as an ApiKey on a fresh client and hit a protected endpoint.
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", payload.plaintext);
        var response = await client.GetAsync("/api/v1/scripts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Audit_ContainsLoginEvent_AfterSuccessfulAuth()
    {
        var admin = await AuthedAs("admin", "admin");
        var response = await admin.GetAsync("/api/v1/audit?pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AuditLogEntryDto>>();
        body!.Items.Should().Contain(e => e.action == "auth.login");
    }

    [Fact]
    public async Task Alerts_CreateRule_AsAdmin()
    {
        var admin = await AuthedAs("admin", "admin");
        var response = await admin.PostAsJsonAsync("/api/v1/alerts/rules",
            new CreateAlertRuleRequest(
                Name: "smoke-cpu",
                MetricKey: "host.cpu.percent",
                Operator: AlertOperator.GreaterThan,
                Threshold: 90,
                WindowMinutes: 5,
                Aggregation: AlertAggregation.Avg,
                WebhookUrl: null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Alerts_CreateRule_Rejects_ViewerRole()
    {
        var viewer = await AuthedAs("viewer", "viewer");
        var response = await viewer.PostAsJsonAsync("/api/v1/alerts/rules",
            new CreateAlertRuleRequest("viewer-attempt", "cpu", AlertOperator.GreaterThan,
                50, 5, AlertAggregation.Avg, null));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Schedules_Create_WithValidCron()
    {
        var admin = await AuthedAs("admin", "admin");
        var scripts = await admin.GetFromJsonAsync<List<ScriptDto>>("/api/v1/scripts");
        var scriptId = scripts!.First().Id;

        var response = await admin.PostAsJsonAsync("/api/v1/schedules",
            new CreateScheduleRequest(scriptId, "smoke-hourly", "0 * * * *", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ScheduleDto>();
        body!.nextRunAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Schedules_Create_WithInvalidCron_Returns400()
    {
        var admin = await AuthedAs("admin", "admin");
        var scripts = await admin.GetFromJsonAsync<List<ScriptDto>>("/api/v1/scripts");
        var scriptId = scripts!.First().Id;

        var response = await admin.PostAsJsonAsync("/api/v1/schedules",
            new CreateScheduleRequest(scriptId, "bad", "not a cron", null));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
