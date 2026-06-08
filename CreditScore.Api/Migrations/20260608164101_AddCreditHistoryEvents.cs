using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditScore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditHistoryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditHistoryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreditHistoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DebtId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    EventType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditHistoryEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditHistoryEvents_CreditHistories_CreditHistoryId",
                        column: x => x.CreditHistoryId,
                        principalTable: "CreditHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CreditHistoryEvents_CreditHistoryId_DebtId_EventType",
                table: "CreditHistoryEvents",
                columns: new[] { "CreditHistoryId", "DebtId", "EventType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditHistoryEvents");
        }
    }
}
