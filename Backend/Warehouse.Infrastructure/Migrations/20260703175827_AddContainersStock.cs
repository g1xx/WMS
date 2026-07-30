using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContainersStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContainerId",
                table: "Stocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "AddressBarcode", "Aisle", "Floor", "Level", "Position", "Rack", "Sector", "Type", "WarehouseCode" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000005"), "HZA301", "", 3, "", "", "", "ConveyorDrop", 3, "MAIN" },
                    { new Guid("60000000-0000-0000-0000-000000000006"), "HZA302", "", 3, "", "", "", "ConveyorDrop", 3, "MAIN" },
                    { new Guid("70000000-0000-0000-0000-000000000007"), "HZA303", "", 3, "", "", "", "ConveyorDrop", 3, "MAIN" },
                    { new Guid("80000000-0000-0000-0000-000000000008"), "HZA304", "", 3, "", "", "", "ConveyorDrop", 3, "MAIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ContainerId",
                table: "Stocks",
                column: "ContainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_Containers_ContainerId",
                table: "Stocks",
                column: "ContainerId",
                principalTable: "Containers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_Containers_ContainerId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ContainerId",
                table: "Stocks");

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("60000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000008"));

            migrationBuilder.DropColumn(
                name: "ContainerId",
                table: "Stocks");
        }
    }
}
