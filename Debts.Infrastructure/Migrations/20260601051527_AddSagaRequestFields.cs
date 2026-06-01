using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Debts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "DebtCreationSagaStates",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "ResponseAddress",
                table: "DebtCreationSagaStates",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "DebtCreationSagaStates");

            migrationBuilder.DropColumn(
                name: "ResponseAddress",
                table: "DebtCreationSagaStates");
        }
    }
}
