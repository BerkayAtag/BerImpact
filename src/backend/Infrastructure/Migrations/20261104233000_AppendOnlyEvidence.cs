using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BerImpact.Infrastructure.Migrations;

[DbContext(typeof(PlatformDb))]
[Migration("20261104233000_AppendOnlyEvidence")]
public sealed class AppendOnlyEvidence : Migration
{
    protected override void Up(MigrationBuilder b)
    {
        b.Sql("""
            CREATE FUNCTION berimpact_reject_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'BerImpact evidence is append-only'; END; $$;
            CREATE TRIGGER audit_append_only BEFORE UPDATE OR DELETE OR TRUNCATE ON "Audit" FOR EACH STATEMENT EXECUTE FUNCTION berimpact_reject_mutation();
            CREATE TRIGGER assessment_append_only BEFORE UPDATE OR DELETE OR TRUNCATE ON "Assessments" FOR EACH STATEMENT EXECUTE FUNCTION berimpact_reject_mutation();
            CREATE TRIGGER approval_append_only BEFORE UPDATE OR DELETE OR TRUNCATE ON "Approvals" FOR EACH STATEMENT EXECUTE FUNCTION berimpact_reject_mutation();
            """);
    }
    protected override void Down(MigrationBuilder b)
    {
        b.Sql("""
            DROP TRIGGER audit_append_only ON "Audit";
            DROP TRIGGER assessment_append_only ON "Assessments";
            DROP TRIGGER approval_append_only ON "Approvals";
            DROP FUNCTION berimpact_reject_mutation();
            """);
    }
}
