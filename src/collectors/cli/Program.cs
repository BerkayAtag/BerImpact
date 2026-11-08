using System.Text.Json;
using System.Text.Json.Serialization;
using BerImpact.Connectors;
using BerImpact.Sdk;

if (args.Length != 4) {
    Console.Error.WriteLine("Usage: BerImpact.Collector <https-api-endpoint> <source-namespace> <token-file> <output-json-file>");
    return 2;
}
try {
    var endpoint = new Uri(args[0], UriKind.Absolute);
    var source = args[1];
    if (source.Length is < 1 or > 120) throw new ArgumentException("Source namespace must be 1..120 characters.");
    // File credentials are never echoed or included in request diagnostics.
    var token = (await File.ReadAllTextAsync(args[2])).Trim();
    if (File.Exists(args[3])) throw new IOException("Output already exists; use a new filename.");
    using var handler = new HttpClientHandler { AllowAutoRedirect = false };
    using var client = new HttpClient(handler);
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(75));
    var connector = new ZabbixConnector(client, endpoint, token, 1000);
    var records = new List<AssetRecord>();
    await foreach (var page in connector.DiscoverAsync(null, deadline.Token)) records.AddRange(page.Records);
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    await using var file = new FileStream(args[3], FileMode.CreateNew, FileAccess.Write, FileShare.None);
    await JsonSerializer.SerializeAsync(file, new ImportBatch(source, false, records), options, deadline.Token);
    Console.WriteLine($"Collected {records.Count} source records. Import the JSON through the authorized BerImpact console. No source writes occurred.");
    return 0;
} catch (Exception e) when (e is ArgumentException or UriFormatException or HttpRequestException or IOException or JsonException or OperationCanceledException or InvalidOperationException) {
    Console.Error.WriteLine($"Collection failed ({e.GetType().Name}). No successful inventory export was reported. Check endpoint, file permissions, API role and the 1000-record bound.");
    return 1;
}
