using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLocationFromPutawayItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PutawayTaskItems_Locations_DestinationLocationId",
                table: "PutawayTaskItems");

            migrationBuilder.DropIndex(
                name: "IX_PutawayTaskItems_DestinationLocationId",
                table: "PutawayTaskItems");

            migrationBuilder.DropColumn(
                name: "DestinationLocationId",
                table: "PutawayTaskItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DestinationLocationId",
                table: "PutawayTaskItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTaskItems_DestinationLocationId",
                table: "PutawayTaskItems",
                column: "DestinationLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PutawayTaskItems_Locations_DestinationLocationId",
                table: "PutawayTaskItems",
                column: "DestinationLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
