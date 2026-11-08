using BerImpact.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "BerImpact Worker");
builder.Services.AddPlatform(builder.Configuration);
builder.Services.AddHostedService<AnalysisWorker>();
builder.Logging.AddJsonConsole();
await builder.Build().RunAsync();
