using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BerImpact.Application;
using BerImpact.Domain;
using BerImpact.Infrastructure;
using BerImpact.Sdk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace BerImpact.IntegrationTests;

public sealed class PlatformFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    private string adminConnection = "";
    private readonly string database = "berimpact_test_" + Guid.NewGuid().ToString("N");
    public string Password { get; } = "Bi!" + Guid.NewGuid().ToString("N") + "A1";

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("BERIMPACT_TEST_DATABASE") ?? throw new InvalidOperationException("BERIMPACT_TEST_DATABASE must point to an isolated local PostgreSQL instance.");
        var cs = new NpgsqlConnectionStringBuilder(adminConnection);
        if (cs.Host is not ("127.0.0.1" or "localhost")) throw new InvalidOperationException("Integration tests only use loopback PostgreSQL.");
        await using (var conn = new NpgsqlConnection(adminConnection)) { await conn.OpenAsync(); await using var cmd = new NpgsqlCommand($"CREATE DATABASE {database}", conn); await cmd.ExecuteNonQueryAsync(); }
        cs.Database = database;
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseSetting("ConnectionStrings:Database", cs.ConnectionString).UseSetting("RunWorker", "false"));
        using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PlatformDb>(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All) Assert.True((await roles.CreateAsync(new(role))).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<PlatformUser>>();
        foreach (var (name, role) in new[] { ("manager", Roles.Admin), ("reader", Roles.Reader), ("approver", Roles.Approver), ("analyst", Roles.Analyst) }) {
            var user = new PlatformUser { UserName = name }; Assert.True((await users.CreateAsync(user, Password)).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
    }
    public HttpClient Client() => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
    public static async Task SetCsrf(HttpClient client) { var json = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/csrf"); client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", json.GetProperty("token").GetString()); }
    public async Task<HttpClient> Login(string username) { var client = Client(); await SetCsrf(client); var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password = Password }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); await SetCsrf(client); return client; }
    public async Task DisposeAsync() {
        await Factory.DisposeAsync(); NpgsqlConnection.ClearAllPools();
        await using var conn = new NpgsqlConnection(adminConnection); await conn.OpenAsync(); await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {database} WITH (FORCE)", conn); await cmd.ExecuteNonQueryAsync();
    }
}

public sealed class PlatformTests(PlatformFixture fixture) : IClassFixture<PlatformFixture>
{
    [Fact] public async Task Anonymous_access_and_csrf_are_rejected() {
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/assets")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/assets/import", new {})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader", password = fixture.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/health/ready")).StatusCode);
    }

    [Fact] public async Task Reader_and_analyst_cannot_import_or_create_changes() {
        foreach (var role in new[] { "reader", "analyst" }) {
            using var client = await fixture.Login(role);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/assets/import", new {})).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/changes", new {})).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/audit/events")).StatusCode);
        }
    }

    [Fact] public async Task Inventory_change_analysis_simulation_governance_report_roundtrip() {
        using var client = await fixture.Login("manager"); var source = "test-" + Guid.NewGuid().ToString("N"); var observed = DateTime.UtcNow.AddMinutes(-1);
        AssetRecord[] records = [new("guid-a", "same-name", "Computer", "Windows 11", "M1", "A", false, true, EvidenceState.Verified, observed), new("guid-b", "same-name", "Computer", "Windows 11", "M2", "B", false, true, EvidenceState.Verified, observed), new("service", "Payments", "Service", "", "", "DC", true, true, EvidenceState.Verified, observed)];
        var batch = new ImportBatch(source, true, records); var importKey = Guid.NewGuid().ToString();
        var first = await Send(client, "/assets/import", batch, importKey); Assert.Equal(HttpStatusCode.OK, first.StatusCode); var receipt = await first.Content.ReadFromJsonAsync<ImportReceipt>(Json.Options);
        var retry = await Send(client, "/assets/import", batch, importKey); Assert.Equal(receipt!.Id, (await retry.Content.ReadFromJsonAsync<ImportReceipt>(Json.Options))!.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, "/assets/import", batch with { Records = records.Take(1).ToArray() }, importKey)).StatusCode);
        var assetsResponse = await client.GetFromJsonAsync<JsonElement>($"/api/v1/assets?test=true&search={source}"); var assets = Json.Read<Asset[]>(assetsResponse.GetProperty("items").GetRawText()); Assert.Equal(3, assets.Length); Assert.Equal(2, assets.Count(a => a.Name == "same-name"));
        var a = assets.Single(a => a.ExternalId == "guid-a"); var service = assets.Single(a => a.ExternalId == "service");
        var live = await client.GetFromJsonAsync<JsonElement>($"/api/v1/assets?test=false&search={source}"); Assert.Equal(0, live.GetProperty("total").GetInt32());
        var oldBatch = batch with { Records = [records[0] with { Name = "old-name", ObservedAt = observed.AddDays(-10) }] }; Assert.Equal(HttpStatusCode.OK, (await Send(client, "/assets/import", oldBatch)).StatusCode);
        var assetNow = await client.GetFromJsonAsync<Asset>($"/api/v1/assets/{a.Id}", Json.Options); Assert.Equal("same-name", assetNow!.Name);
        var invalid = batch with { Records = [records[0] with { ExternalId = "new", Name = "valid" }, records[0] with { ExternalId = "bad", Name = "" }] }; Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/assets/import", invalid)).StatusCode);
        var edge = await Send(client, "/relationships", new { sourceId = service.Id, targetId = a.Id, type = "DependsOn", source, evidence = "Service owner dependency record", state = "Verified", observedAt = observed }); Assert.Equal(HttpStatusCode.Created, edge.StatusCode);
        var id = Guid.NewGuid(); var body = new { id, title = "NIC test change", description = "Isolated test", type = "RegistryModification", before = new { TCPChecksumOffloadIPv4 = 1 }, after = new { TCPChecksumOffloadIPv4 = 0 }, target = new TargetFilter(Source: source, AssetIds: [a.Id], IsTest: true), rollbackPlan = "Restore backed-up setting", rollbackDifficulty = 20, technicalSensitivity = 40, factorEvidence = "Lab test evidence", successCriteria = "No packet loss", version = 1 };
        var create = await Send(client, "/changes", body); Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var jobKey = Guid.NewGuid().ToString(); var queued = await Send(client, $"/changes/{id}/assessments", new { scenarios = Array.Empty<Scenario>() }, jobKey); Assert.Equal(HttpStatusCode.Accepted, queued.StatusCode);
        var job = await queued.Content.ReadFromJsonAsync<AnalysisJob>(Json.Options);
        var sameJob = await Send(client, $"/changes/{id}/assessments", new { scenarios = Array.Empty<Scenario>() }, jobKey); Assert.Equal(job!.Id, (await sameJob.Content.ReadFromJsonAsync<AnalysisJob>(Json.Options))!.Id);
        var worker = new AnalysisWorker(fixture.Factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AnalysisWorker>.Instance); Assert.True(await worker.RunOneAsync(default));
        var state = await client.GetFromJsonAsync<JsonElement>($"/api/v1/jobs/{job.Id}"); Assert.Equal("Completed", state.GetProperty("status").GetString()); var assessmentId = state.GetProperty("assessmentId").GetGuid();
        var assessment = await client.GetFromJsonAsync<Assessment>($"/api/v1/risk/assessments/{assessmentId}", Json.Options); var result = Json.Read<AnalysisResult[]>(assessment!.ResultJson).Single(); Assert.Equal(1, result.DirectCount); Assert.Equal(1, result.IndirectCount); Assert.True(result.ReviewRequired); Assert.Contains(result.Findings, f => f.RuleId == "CRIT-001");
        var snapshot = Json.Read<AnalysisInput>(assessment.SnapshotJson); Assert.Equal(Json.Write(new AnalysisEngine().Analyze(snapshot, assessment.RulesetVersion)), Json.Write(Json.Read<AnalysisResult[]>(assessment.ResultJson)));
        // JSONB canonicalizes property order; snapshot integrity is checked against canonical input serialization.
        Assert.Equal(assessment.SnapshotHash, Json.Hash(Json.Write(snapshot)));
        foreach (var format in new[] { "json", "html", "csv" }) { var report = await client.GetAsync($"/api/v1/reports/{assessmentId}?format={format}"); Assert.Equal(HttpStatusCode.OK, report.StatusCode); Assert.True((await report.Content.ReadAsByteArrayAsync()).Length > 100); }
        var simulation = await Send(client, $"/changes/{id}/assessments", new { scenarios = new[] { new Scenario("All", new(Source: source, IsTest: true)), new Scenario("M1", new(Source: source, Model: "M1", IsTest: true)) } }); Assert.Equal(HttpStatusCode.Accepted, simulation.StatusCode); Assert.True(await worker.RunOneAsync(default));
        var simulationJob = await simulation.Content.ReadFromJsonAsync<AnalysisJob>(Json.Options); var simState = await client.GetFromJsonAsync<JsonElement>($"/api/v1/jobs/{simulationJob!.Id}"); var sim = await client.GetFromJsonAsync<Assessment>($"/api/v1/risk/assessments/{simState.GetProperty("assessmentId").GetGuid()}", Json.Options); var comparison = Json.Read<AnalysisResult[]>(sim!.ResultJson); Assert.Equal(3, comparison[0].DirectCount); Assert.Equal(1, comparison[1].DirectCount);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, $"/changes/{id}/submit", new { version = 1 })).StatusCode);
        using var approver = await fixture.Login("approver"); Assert.Equal(HttpStatusCode.Conflict, (await Send(approver, $"/changes/{id}/decision", new { assessmentId, version = 1, approved = true, reason = "Review evidence" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(approver, $"/changes/{id}/decision", new { assessmentId, version = 1, approved = false, reason = "Missing history evidence" })).StatusCode);
        var edit = await client.PutAsJsonAsync($"/api/v1/changes/{id}", body with { title = "Revised NIC test change" }, Json.Options); Assert.Equal(HttpStatusCode.OK, edit.StatusCode); var edited = await edit.Content.ReadFromJsonAsync<ChangeRequest>(Json.Options); Assert.Equal(2, edited!.Version); Assert.Equal(ChangeState.Draft, edited.State);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/changes/{id}", body, Json.Options)).StatusCode);
        var staleQueued = await Send(client, $"/changes/{id}/assessments", new { scenarios = Array.Empty<Scenario>() });
        Assert.Equal(HttpStatusCode.Accepted, staleQueued.StatusCode);
        var staleJob = await staleQueued.Content.ReadFromJsonAsync<AnalysisJob>(Json.Options);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/changes/{id}", body with { version = 2, title = "Changed after queueing" }, Json.Options)).StatusCode);
        Assert.True(await worker.RunOneAsync(default));
        var failed = await client.GetFromJsonAsync<JsonElement>($"/api/v1/jobs/{staleJob!.Id}");
        Assert.Equal("Failed", failed.GetProperty("status").GetString());
        Assert.Contains("version changed", failed.GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, $"/changes/{id}/assessments", new { scenarios = new[] { new Scenario("Invalid live scope", new(IsTest: false)) } })).StatusCode);
        using var scope = fixture.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PlatformDb>(); Assert.True(await db.Audit.AnyAsync(a => a.Action == "AnalysisCompleted"));
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE \"Audit\" SET \"Detail\" = 'tamper'"));
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("TRUNCATE \"Assessments\" CASCADE"));
    }

    [Fact] public async Task Logout_invalidates_browser_session() {
        using var client = await fixture.Login("reader"); Assert.Equal(HttpStatusCode.NoContent, (await Send(client, "/auth/logout", new {})).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string path, object body, string? key = null) {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1" + path) { Content = JsonContent.Create(body, options: Json.Options) }; request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString()); return client.SendAsync(request);
    }
}
