using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPickTasksAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickTask",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sector = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedWorkerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickTask_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickTask_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickTaskItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PickTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredQuantity = table.Column<int>(type: "integer", nullable: false),
                    PickedQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTaskItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickTaskItem_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickTaskItem_PickTask_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickTaskItem_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickTask_ContainerId",
                table: "PickTask",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTask_OrderId",
                table: "PickTask",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskItem_LocationId",
                table: "PickTaskItem",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskItem_PickTaskId",
                table: "PickTaskItem",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskItem_ProductId",
                table: "PickTaskItem",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickTaskItem");

            migrationBuilder.DropTable(
                name: "PickTask");
        }
    }
}
