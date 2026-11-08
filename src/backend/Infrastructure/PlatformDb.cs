using BerImpact.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BerImpact.Infrastructure;

public sealed class PlatformUser : IdentityUser { }

public sealed class PlatformDb(DbContextOptions<PlatformDb> options) : IdentityDbContext<PlatformUser>(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetRelationship> Relationships => Set<AssetRelationship>();
    public DbSet<ChangeRequest> Changes => Set<ChangeRequest>();
    public DbSet<AnalysisJob> Jobs => Set<AnalysisJob>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<ApprovalDecision> Approvals => Set<ApprovalDecision>();
    public DbSet<AuditEvent> Audit => Set<AuditEvent>();
    public DbSet<ImportReceipt> Imports => Set<ImportReceipt>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Asset>(e => {
            e.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique();
            e.HasIndex(x => new { x.IsTest, x.Name });
            e.HasIndex(x => new { x.Location, x.Os });
            e.HasIndex(x => x.ObservedAt);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        b.Entity<AssetRelationship>(e => {
            e.HasOne<Asset>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Asset>().WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SourceId, x.TargetId, x.Type, x.Source }).IsUnique();
            e.HasIndex(x => x.TargetId);
        });
        b.Entity<ChangeRequest>(e => {
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.BeforeJson).HasColumnType("jsonb");
            e.Property(x => x.AfterJson).HasColumnType("jsonb");
            e.Property(x => x.TargetJson).HasColumnType("jsonb");
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AnalysisJob>(e => {
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.ScenariosJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.RequestedBy, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasOne<ChangeRequest>().WithMany().HasForeignKey(x => x.ChangeId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Assessment>(e => {
            e.Property(x => x.SnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.ResultJson).HasColumnType("jsonb");
            e.HasIndex(x => x.JobId).IsUnique();
            e.HasIndex(x => new { x.ChangeId, x.CreatedAt });
            e.HasOne<AnalysisJob>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ChangeRequest>().WithMany().HasForeignKey(x => x.ChangeId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ApprovalDecision>(e => {
            e.HasOne<ChangeRequest>().WithMany().HasForeignKey(x => x.ChangeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Assessment>().WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ChangeId, x.ChangeVersion, x.ActorId }).IsUnique();
        });
        b.Entity<AuditEvent>().HasIndex(x => x.At);
        b.Entity<ImportReceipt>().HasIndex(x => new { x.Source, x.IdempotencyKey }).IsUnique();
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties().Where(p => p.ClrType == typeof(string) && p.GetColumnType() != "jsonb"))
                if (prop.GetMaxLength() == null) prop.SetMaxLength(4000);
        b.Entity<Asset>().Property(x => x.Source).HasMaxLength(120);
        b.Entity<Asset>().Property(x => x.ExternalId).HasMaxLength(200);
        b.Entity<Asset>().Property(x => x.Name).HasMaxLength(200);
        b.Entity<AnalysisJob>().Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.Entity<ImportReceipt>().Property(x => x.IdempotencyKey).HasMaxLength(100);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.Entries().Any(e => e.Entity is AuditEvent or Assessment or ApprovalDecision && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Evidence and audit records are append-only.");
        return base.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DesignDbFactory : IDesignTimeDbContextFactory<PlatformDb>
{
    public PlatformDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PlatformDb>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? "Host=localhost;Database=berimpact_design").Options);
}
