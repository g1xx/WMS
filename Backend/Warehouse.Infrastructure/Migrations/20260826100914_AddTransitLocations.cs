using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransitLocations : Migration
    {
        /// <inheritdoc />
        // `dotnet ef migrations add` warned "may result in the loss of data" for this one.
        // Reviewed statement by statement: it's a false alarm. Nothing is dropped or
        // narrowed — one nullable column is added, the eight seeded HasData rows get that
        // column set to null (no-ops EF can't recognise as such, and the source of the
        // warning), and a filtered unique index is created. The Type = 5 in the filter is
        // LocationType.Transit as of this migration; it is deliberately a literal, because
        // a migration records history and must not shift if the enum ever does.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedWorkerId",
                table: "Locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000005"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("60000000-0000-0000-0000-000000000006"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000007"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000008"),
                column: "AssignedWorkerId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_AssignedWorkerId",
                table: "Locations",
                column: "AssignedWorkerId",
                unique: true,
                filter: "\"Type\" = 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Locations_AssignedWorkerId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "AssignedWorkerId",
                table: "Locations");
        }
    }
}
