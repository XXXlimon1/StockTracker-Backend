using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddChangePercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChangePercent",
                table: "StockPrices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangePercent",
                table: "StockPrices");
        }
    }
}
