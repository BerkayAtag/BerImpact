using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BerImpact.Application;
using BerImpact.Domain;
using BerImpact.Infrastructure;
using BerImpact.Sdk;
using Xunit;

namespace BerImpact.IntegrationTests;

public sealed class RegressionTests(PlatformFixture fixture) : IClassFixture<PlatformFixture>
{
    private static Task<HttpResponseMessage> Post(HttpClient c, string path, object value) {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1" + path) { Content = JsonContent.Create(value, options: Json.Options) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); return c.SendAsync(request);
    }
    private static AssetRecord Record(string id, DateTime at) => new(id, id, "Computer", "Windows", "M1", "Lab", false, true, EvidenceState.Verified, at);

    [Fact] public async Task Null_import_record_returns_400_and_does_not_insert_other_records() {
        using var c = await fixture.Login("manager"); var source = Guid.NewGuid().ToString();
        var response = await Post(c, "/assets/import", new { source, isTest = true, records = new object?[] { Record("valid", DateTime.UtcNow), null } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var inventory = await c.GetFromJsonAsync<JsonElement>($"/api/v1/assets?test=true&search={source}");
        Assert.Equal(0, inventory.GetProperty("total").GetInt32());
    }
    [Fact] public async Task Equal_timestamp_conflict_is_atomic_and_preserves_verified_evidence() {
        using var c = await fixture.Login("manager"); var source = Guid.NewGuid().ToString(); var record = Record("stable", DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(HttpStatusCode.OK, (await Post(c, "/assets/import", new ImportBatch(source, true, [record]))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(c, "/assets/import", new ImportBatch(source, true, [record with { ExternalId = "new" }, record with { Name = "conflict" }]))).StatusCode);
        var inventory = await c.GetFromJsonAsync<JsonElement>($"/api/v1/assets?test=true&search={source}");
        Assert.Equal(1, inventory.GetProperty("total").GetInt32());
        Assert.Equal("stable", inventory.GetProperty("items")[0].GetProperty("name").GetString());
    }
    [Fact] public async Task Relationship_refresh_preserves_identity_and_rejects_conflicting_evidence() {
        using var c = await fixture.Login("manager"); var source = Guid.NewGuid().ToString(); var at = DateTime.UtcNow.AddMinutes(-2);
        Assert.Equal(HttpStatusCode.OK, (await Post(c, "/assets/import", new ImportBatch(source, true, [Record("a", at), Record("b", at)]))).StatusCode);
        var inventory = await c.GetFromJsonAsync<JsonElement>($"/api/v1/assets?test=true&search={source}");
        var assets = Json.Read<Asset[]>(inventory.GetProperty("items").GetRawText());
        var body = new { sourceId = assets[0].Id, targetId = assets[1].Id, type = "DependsOn", source, evidence = "Old", state = "Unknown", observedAt = at };
        var first = await Post(c, "/relationships", body); Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var edge = await first.Content.ReadFromJsonAsync<AssetRelationship>(Json.Options);
        var update = await Post(c, "/relationships", body with { evidence = "Current", state = "Verified", observedAt = at.AddMinutes(1) });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var refreshed = await update.Content.ReadFromJsonAsync<AssetRelationship>(Json.Options); Assert.Equal(edge!.Id, refreshed!.Id); Assert.Equal(EvidenceState.Verified, refreshed.State);
        var stale = await Post(c, "/relationships", body); Assert.Equal("Current", (await stale.Content.ReadFromJsonAsync<AssetRelationship>(Json.Options))!.Evidence);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(c, "/relationships", body with { evidence = "Conflict", observedAt = at.AddMinutes(1) })).StatusCode);
    }
    [Fact] public async Task Target_ID_validation_and_change_partition_are_enforced() {
        using var c = await fixture.Login("manager"); var id = Guid.NewGuid(); var assetId = Guid.NewGuid();
        var body = new { id, title = "Regression test", description = "Test", type = "Custom", before = new {}, after = new {}, target = new TargetFilter(IsTest: true), rollbackPlan = "Restore", successCriteria = "Healthy", factorEvidence = "", version = 1 };
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(c, "/changes", body with { target = new(AssetIds: [Guid.Empty], IsTest: true) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(c, "/changes", body with { target = new(AssetIds: [assetId, assetId], IsTest: true) })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(c, "/changes", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v1/changes/{id}", body with { target = new(IsTest: false) }, Json.Options)).StatusCode);
    }
    [Fact] public async Task Unsafe_report_content_is_encoded_and_invalid_format_rejected() {
        using var c = await fixture.Login("manager"); var id = Guid.NewGuid();
        var body = new { id, title = "<script>alert(1)</script>", description = "Test", type = "Custom", before = new {}, after = new {}, target = new TargetFilter(IsTest: true), rollbackPlan = "Restore", successCriteria = "Healthy", factorEvidence = "" };
        Assert.Equal(HttpStatusCode.Created, (await Post(c, "/changes", body)).StatusCode);
        var response = await Post(c, $"/changes/{id}/assessments", new { scenarios = new[] { new Scenario("=DANGEROUS()", new(IsTest: true)) } });
        var job = await response.Content.ReadFromJsonAsync<AnalysisJob>(Json.Options);
        var worker = new AnalysisWorker(fixture.Factory.Services.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<AnalysisWorker>.Instance);
        Assert.True(await worker.RunOneAsync(default));
        var status = await c.GetFromJsonAsync<JsonElement>($"/api/v1/jobs/{job!.Id}"); var assessment = status.GetProperty("assessmentId").GetGuid();
        var html = await c.GetStringAsync($"/api/v1/reports/{assessment}?format=html"); Assert.DoesNotContain("<script>", html); Assert.Contains("&lt;script&gt;", html);
        var csv = await c.GetStringAsync($"/api/v1/reports/{assessment}?format=csv"); Assert.Contains("'=DANGEROUS()", csv);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v1/reports/{assessment}?format=exe")).StatusCode);
    }
}
