using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StockTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPriceHistoryAndAlertTriggeredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockPrices_Ticker",
                table: "StockPrices");

            migrationBuilder.AddColumn<DateTime>(
                name: "TriggeredAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockPriceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ticker = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPriceHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockPrices_Ticker",
                table: "StockPrices",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockPriceHistories_Ticker_RecordedAt",
                table: "StockPriceHistories",
                columns: new[] { "Ticker", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_StockPrices_Ticker",
                table: "StockPrices");

            migrationBuilder.DropColumn(
                name: "TriggeredAt",
                table: "Alerts");

            migrationBuilder.CreateIndex(
                name: "IX_StockPrices_Ticker",
                table: "StockPrices",
                column: "Ticker");
        }
    }
}
