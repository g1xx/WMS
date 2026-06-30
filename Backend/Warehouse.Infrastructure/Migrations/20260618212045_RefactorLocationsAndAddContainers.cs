using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorLocationsAndAddContainers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Locations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MaxWeightCapacityKg = table.Column<decimal>(type: "numeric", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Containers_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "AddressBarcode", "Aisle", "Floor", "Level", "Position", "Rack", "Sector", "Type", "WarehouseCode" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "tgn1", "", 1, "", "", "", "Rampa", 2, "MAIN" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "tgn2", "", 2, "", "", "", "Rampa", 2, "MAIN" },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "tgn3", "", 3, "", "", "", "Rampa", 2, "MAIN" },
                    { new Guid("40000000-0000-0000-0000-000000000004"), "tgn4", "", 4, "", "", "", "Rampa", 2, "MAIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Containers_LocationId",
                table: "Containers",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Locations");
        }
    }
}
