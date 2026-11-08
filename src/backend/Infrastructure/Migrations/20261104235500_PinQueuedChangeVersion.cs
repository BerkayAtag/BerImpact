using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BerImpact.Infrastructure.Migrations;

[DbContext(typeof(PlatformDb))]
[Migration("20261104235500_PinQueuedChangeVersion")]
public sealed class PinQueuedChangeVersion : Migration
{
    protected override void Up(MigrationBuilder b) => b.AddColumn<int>(name: "ChangeVersion", table: "Jobs", type: "integer", nullable: false, defaultValue: 0);
    protected override void Down(MigrationBuilder b) => b.DropColumn(name: "ChangeVersion", table: "Jobs");
}
