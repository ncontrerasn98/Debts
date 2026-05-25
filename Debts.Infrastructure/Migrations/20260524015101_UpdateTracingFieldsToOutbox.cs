using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Debts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTracingFieldsToOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentSpanId",
                table: "OutboxMessages");

            migrationBuilder.RenameColumn(
                name: "TraceId",
                table: "OutboxMessages",
                newName: "TraceParent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TraceParent",
                table: "OutboxMessages",
                newName: "TraceId");

            migrationBuilder.AddColumn<string>(
                name: "ParentSpanId",
                table: "OutboxMessages",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
