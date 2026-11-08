using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BerImpact.Infrastructure.Migrations;

[DbContext(typeof(PlatformDb))]
[Migration("20261104234000_RuntimePrivileges")]
public sealed class RuntimePrivileges : Migration
{
    protected override void Up(MigrationBuilder b) => b.Sql("""
        DO $$ BEGIN
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'berimpact_runtime') THEN
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO berimpact_runtime;
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO berimpact_runtime;
            REVOKE UPDATE, DELETE ON "Audit", "Assessments", "Approvals" FROM berimpact_runtime;
            REVOKE INSERT, UPDATE, DELETE ON "__EFMigrationsHistory" FROM berimpact_runtime;
        END IF;
        END $$;
        """);
    protected override void Down(MigrationBuilder b) { }
}
