using System.Net;
using System.Text;
using BerImpact.Connectors;
using BerImpact.Domain;
using Xunit;

namespace BerImpact.Tests;

public sealed class ConnectorTests
{
    private sealed class Handler(string response) : HttpMessageHandler {
        public string? Body; public string? Authorization;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken); Authorization = request.Headers.Authorization?.Scheme;
            return new(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
    [Fact] public async Task Host_contract_preserves_ids_and_unknown_availability() {
        var handler = new Handler("""{"jsonrpc":"2.0","result":[{"hostid":"42","host":"device","name":"Display","inventory":{"os":"Windows","model":"M1","location":"Branch"}}],"id":1}""");
        using var http = new HttpClient(handler); var connector = new ZabbixConnector(http, new("https://zabbix.test/api_jsonrpc.php"), "test-token");
        await foreach (var page in connector.DiscoverAsync(null, default)) { var record = Assert.Single(page.Records); Assert.Equal("42", record.ExternalId); Assert.Null(record.Online); Assert.Equal(EvidenceState.Inferred, record.Evidence); }
        Assert.Contains("host.get", handler.Body); Assert.DoesNotContain("host.update", handler.Body); Assert.Equal("Bearer", handler.Authorization);
    }
    [Fact] public async Task Api_error_marks_unhealthy_without_exposing_response() {
        using var http = new HttpClient(new Handler("""{"error":{"data":"sensitive server text"}}"""));
        var health = await new ZabbixConnector(http, new("https://zabbix.test/api_jsonrpc.php"), "test-token").TestConnectionAsync(default); Assert.False(health.Healthy); Assert.DoesNotContain("sensitive", health.Message);
    }
    [Fact] public async Task Insecure_endpoint_is_rejected_before_network() {
        using var http = new HttpClient(new Handler("{}")); await Assert.ThrowsAsync<ArgumentException>(() => new ZabbixConnector(http, new("http://zabbix.test/"), "test-token").TestConnectionAsync(default));
    }
    [Fact] public async Task Excess_results_fail_without_returning_partial_inventory() {
        using var http = new HttpClient(new Handler("""{"result":[{},{}]}""")); var connector = new ZabbixConnector(http, new("https://zabbix.test/"), "test-token", 1);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await foreach (var _ in connector.DiscoverAsync(null, default)) Assert.Fail("No page should be yielded"); });
    }
    [Theory][InlineData("{\"result\":null}")][InlineData("[]")][InlineData("{\"result\":\"unexpected\"}")]
    public async Task Invalid_response_shape_cannot_report_a_healthy_connector(string json) {
        using var http = new HttpClient(new Handler(json));
        Assert.False((await new ZabbixConnector(http, new("https://zabbix.test/"), "test-token").TestConnectionAsync(default)).Healthy);
    }
}
