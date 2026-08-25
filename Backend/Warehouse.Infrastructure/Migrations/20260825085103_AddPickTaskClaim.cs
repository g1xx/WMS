using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPickTaskClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "PickTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_Sector_Status_CreatedAt",
                table: "PickTasks",
                columns: new[] { "Sector", "Status", "CreatedAt" });

            // Establishes the invariant the claim model depends on: before this migration
            // AssignedWorkerId was only ever set together with Status = InProgress, so a New
            // row with an assignee shouldn't exist — but if one did, it would now be invisible
            // forever. The claim query skips it (AssignedWorkerId IS NOT NULL) and the expiry
            // sweep can't free it (ClaimedAt IS NULL, which the backfill above leaves it as).
            // Status 0 = PickTaskStatus.New.
            migrationBuilder.Sql(@"
                UPDATE ""PickTasks""
                SET ""AssignedWorkerId"" = NULL
                WHERE ""Status"" = 0 AND ""AssignedWorkerId"" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PickTasks_Sector_Status_CreatedAt",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "PickTasks");
        }
    }
}
