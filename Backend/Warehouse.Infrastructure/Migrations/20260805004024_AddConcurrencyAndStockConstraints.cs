using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyAndStockConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_ContainerId",
                table: "PickTasks");

            // xmin is a Postgres system column present implicitly on every table already;
            // it must NOT be added as a real column. EF just needs the shadow property
            // (configured via UseXminAsConcurrencyToken) to read it for optimistic concurrency.
            migrationBuilder.AddColumn<int>(
                name: "MissingQuantity",
                table: "PickTaskItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId_LocationId",
                table: "Stocks",
                columns: new[] { "ProductId", "LocationId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_PhysicalQuantity_NonNegative",
                table: "Stocks",
                sql: "\"PhysicalQuantity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_ReservedNotExceedingPhysical",
                table: "Stocks",
                sql: "\"ReservedQuantity\" <= \"PhysicalQuantity\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_ReservedQuantity_NonNegative",
                table: "Stocks",
                sql: "\"ReservedQuantity\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_ContainerId",
                table: "PickTasks",
                column: "ContainerId",
                unique: true,
                filter: "\"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductId_LocationId",
                table: "Stocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_PhysicalQuantity_NonNegative",
                table: "Stocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_ReservedNotExceedingPhysical",
                table: "Stocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_ReservedQuantity_NonNegative",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_ContainerId",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "MissingQuantity",
                table: "PickTaskItems");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId",
                table: "Stocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_ContainerId",
                table: "PickTasks",
                column: "ContainerId");
        }
    }
}
