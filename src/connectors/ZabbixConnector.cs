using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BerImpact.Domain;
using BerImpact.Sdk;

namespace BerImpact.Connectors;

// Configure a dedicated HttpClient with redirects disabled and normal certificate validation.
// Only an explicitly supplied HTTPS endpoint is contacted. This adapter is not auto-registered.
public sealed class ZabbixConnector(HttpClient http, Uri endpoint, string token, int limit = 1000) : IReadOnlyConnector
{
    public ConnectorManifest Manifest => new("zabbix", "Zabbix 7.x host inventory", "0.1.1", ["Hosts", "BoundedFullSync"], ["host.get"], true, "AdapterOnly");

    private void Validate()
    {
        if (endpoint.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Fragment) || !string.IsNullOrEmpty(endpoint.Query) || string.IsNullOrWhiteSpace(token) || limit is < 1 or > 5000)
            throw new ArgumentException("Explicit HTTPS endpoint, token and limit 1..5000 required.");
    }

    public async Task<ConnectorHealth> TestConnectionAsync(CancellationToken cancellationToken)
    {
        Validate();
        try { using var result = await ReadAsync("host.get", new { output = new[] { "hostid" }, limit = 1 }, cancellationToken); return new(true, "host.get permission verified; inventory freshness remains separate.", DateTime.UtcNow); }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException) { return new(false, "Zabbix read failed; inspect endpoint, permissions and availability.", DateTime.UtcNow); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(false, "Zabbix read timed out.", DateTime.UtcNow); }
    }

    public async IAsyncEnumerable<ConnectorPage> DiscoverAsync(string? checkpoint, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Validate();
        if (checkpoint != null) throw new NotSupportedException("Incremental sync is not supported by this adapter.");
        using var result = await ReadAsync("host.get", new { output = new[] { "hostid", "host", "name" }, selectInventory = new[] { "os", "model", "location" }, sortfield = "hostid", limit = limit + 1 }, cancellationToken);
        var rows = result.RootElement.GetProperty("result");
        if (rows.GetArrayLength() > limit) throw new InvalidOperationException("Source exceeds configured full-sync bound; no partial inventory is accepted.");
        static string Text(JsonElement o, string key) => o.ValueKind == JsonValueKind.Object && o.TryGetProperty(key, out var value) ? value.GetString() ?? "" : "";
        var observed = DateTime.UtcNow;
        var records = rows.EnumerateArray().Select(r => {
            r.TryGetProperty("inventory", out var inventory);
            return new AssetRecord(Text(r, "hostid"), Text(r, "name") is { Length: > 0 } name ? name : Text(r, "host"), "Computer", Text(inventory, "os"), Text(inventory, "model"), Text(inventory, "location"), false, null, EvidenceState.Inferred, observed);
        }).ToArray();
        yield return new(records, null);
    }

    private async Task<JsonDocument> ReadAsync(string method, object parameters, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++) {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(20));
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(new { jsonrpc = "2.0", method, @params = parameters, id = 1 }) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if ((response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) && attempt < 2) { await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct); continue; }
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync(4 * 1024 * 1024, timeout.Token);
            var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(timeout.Token));
            if (json.RootElement.ValueKind != JsonValueKind.Object || json.RootElement.TryGetProperty("error", out _) || !json.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array) { json.Dispose(); throw new InvalidOperationException("Zabbix returned an invalid host response."); }
            return json;
        }
    }
}
