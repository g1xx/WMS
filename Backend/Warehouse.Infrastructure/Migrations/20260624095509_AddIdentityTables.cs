using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickTask_Containers_ContainerId",
                table: "PickTask");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTask_Orders_OrderId",
                table: "PickTask");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskItem_Locations_LocationId",
                table: "PickTaskItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskItem_PickTask_PickTaskId",
                table: "PickTaskItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskItem_Products_ProductId",
                table: "PickTaskItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PickTaskItem",
                table: "PickTaskItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PickTask",
                table: "PickTask");

            migrationBuilder.RenameTable(
                name: "PickTaskItem",
                newName: "PickTaskItems");

            migrationBuilder.RenameTable(
                name: "PickTask",
                newName: "PickTasks");

            migrationBuilder.RenameIndex(
                name: "IX_PickTaskItem_ProductId",
                table: "PickTaskItems",
                newName: "IX_PickTaskItems_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTaskItem_PickTaskId",
                table: "PickTaskItems",
                newName: "IX_PickTaskItems_PickTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTaskItem_LocationId",
                table: "PickTaskItems",
                newName: "IX_PickTaskItems_LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTask_OrderId",
                table: "PickTasks",
                newName: "IX_PickTasks_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTask_ContainerId",
                table: "PickTasks",
                newName: "IX_PickTasks_ContainerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PickTaskItems",
                table: "PickTaskItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PickTasks",
                table: "PickTasks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskItems_Locations_LocationId",
                table: "PickTaskItems",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskItems_PickTasks_PickTaskId",
                table: "PickTaskItems",
                column: "PickTaskId",
                principalTable: "PickTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskItems_Products_ProductId",
                table: "PickTaskItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTasks_Containers_ContainerId",
                table: "PickTasks",
                column: "ContainerId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTasks_Orders_OrderId",
                table: "PickTasks",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskItems_Locations_LocationId",
                table: "PickTaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskItems_PickTasks_PickTaskId",
                table: "PickTaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskItems_Products_ProductId",
                table: "PickTaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTasks_Containers_ContainerId",
                table: "PickTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTasks_Orders_OrderId",
                table: "PickTasks");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PickTasks",
                table: "PickTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PickTaskItems",
                table: "PickTaskItems");

            migrationBuilder.RenameTable(
                name: "PickTasks",
                newName: "PickTask");

            migrationBuilder.RenameTable(
                name: "PickTaskItems",
                newName: "PickTaskItem");

            migrationBuilder.RenameIndex(
                name: "IX_PickTasks_OrderId",
                table: "PickTask",
                newName: "IX_PickTask_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTasks_ContainerId",
                table: "PickTask",
                newName: "IX_PickTask_ContainerId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTaskItems_ProductId",
                table: "PickTaskItem",
                newName: "IX_PickTaskItem_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTaskItems_PickTaskId",
                table: "PickTaskItem",
                newName: "IX_PickTaskItem_PickTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_PickTaskItems_LocationId",
                table: "PickTaskItem",
                newName: "IX_PickTaskItem_LocationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PickTask",
                table: "PickTask",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PickTaskItem",
                table: "PickTaskItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTask_Containers_ContainerId",
                table: "PickTask",
                column: "ContainerId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTask_Orders_OrderId",
                table: "PickTask",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskItem_Locations_LocationId",
                table: "PickTaskItem",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskItem_PickTask_PickTaskId",
                table: "PickTaskItem",
                column: "PickTaskId",
                principalTable: "PickTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskItem_Products_ProductId",
                table: "PickTaskItem",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
