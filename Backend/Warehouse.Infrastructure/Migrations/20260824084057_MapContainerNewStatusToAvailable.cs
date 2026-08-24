using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MapContainerNewStatusToAvailable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ContainerStatus.New (0) is removed from the enum — it was never actually
            // distinct from Available (3) in behavior (every read-filter already treated
            // them as equivalent), so every existing New row maps to Available. No schema
            // change: the column was already `integer` and enum membership is a C#-side
            // concept only, not reflected in the DB schema.
            migrationBuilder.Sql("UPDATE \"Containers\" SET \"Status\" = 3 WHERE \"Status\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Lossy and deliberately left that way: once a New row has been remapped to
            // Available, nothing distinguishes it from a container that was always
            // Available. Reversing this migration cannot un-merge that — it's a no-op,
            // not a data restore.
        }
    }
}
