using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPutawayTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PutawayTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sector = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedWorkerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutawayTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PutawayTasks_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PutawayTaskItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PutawayTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    PutAwayQuantity = table.Column<int>(type: "integer", nullable: false),
                    MissingQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutawayTaskItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PutawayTaskItems_Locations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PutawayTaskItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PutawayTaskItems_PutawayTasks_PutawayTaskId",
                        column: x => x.PutawayTaskId,
                        principalTable: "PutawayTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTaskItems_DestinationLocationId",
                table: "PutawayTaskItems",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTaskItems_ProductId",
                table: "PutawayTaskItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTaskItems_PutawayTaskId",
                table: "PutawayTaskItems",
                column: "PutawayTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTasks_ContainerId",
                table: "PutawayTasks",
                column: "ContainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PutawayTaskItems");

            migrationBuilder.DropTable(
                name: "PutawayTasks");
        }
    }
}
