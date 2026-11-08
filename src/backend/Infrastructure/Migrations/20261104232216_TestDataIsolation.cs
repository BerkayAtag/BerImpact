using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TestDataIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTest",
                table: "Changes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
            migrationBuilder.Sql("UPDATE \"Changes\" SET \"IsTest\" = COALESCE((\"TargetJson\"->>'isTest')::boolean, false)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTest",
                table: "Changes");
        }
    }
}

