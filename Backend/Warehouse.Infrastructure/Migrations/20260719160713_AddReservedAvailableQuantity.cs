using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReservedAvailableQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Stocks",
                newName: "ReservedQuantity");

            migrationBuilder.AddColumn<int>(
                name: "PhysicalQuantity",
                table: "Stocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhysicalQuantity",
                table: "Stocks");

            migrationBuilder.RenameColumn(
                name: "ReservedQuantity",
                table: "Stocks",
                newName: "Quantity");
        }
    }
}
