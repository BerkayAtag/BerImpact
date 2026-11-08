using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using BerImpact.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BerImpact.Infrastructure;

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Options) ?? throw new ArgumentException("Empty JSON value.");
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public static class PlatformServices
{
    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Database");
        var file = configuration["DatabasePasswordFile"];
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("ConnectionStrings__Database is required.");
        if (!string.IsNullOrEmpty(file)) {
            var cs = new Npgsql.NpgsqlConnectionStringBuilder(connection) { Password = File.ReadAllText(file).Trim() };
            connection = cs.ConnectionString;
        }
        services.AddDbContext<PlatformDb>(o => o.UseNpgsql(connection, npg => npg.CommandTimeout(30)));
        services.AddSingleton<BerImpact.Application.AnalysisEngine>();
        return services;
    }

    public static void AddAudit(this PlatformDb db, string actor, string action, string entity, string detail = "", string correlation = "") =>
        db.Audit.Add(new AuditEvent { ActorId = actor, Action = action, EntityId = entity, Detail = detail, CorrelationId = correlation });
}

public static class Roles
{
    public const string Admin = "PlatformAdministrator";
    public const string Integration = "IntegrationAdministrator";
    public const string Manager = "ChangeManager";
    public const string Analyst = "RiskAnalyst";
    public const string Approver = "ChangeApprover";
    public const string Operator = "DeploymentOperator";
    public const string Auditor = "Auditor";
    public const string Reader = "ReadOnlyUser";
    public static readonly string[] All = [Admin, Integration, Manager, Analyst, Approver, Operator, Auditor, Reader];
}
