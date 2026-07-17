using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFastCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicReferralSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReferredByUserId",
                table: "StoreSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreReferralLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreReferralLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreReferralLinks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreReferralLinks_Users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreSubscriptions_ReferredByUserId",
                table: "StoreSubscriptions",
                column: "ReferredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreReferralLinks_ReferrerUserId",
                table: "StoreReferralLinks",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreReferralLinks_TenantId",
                table: "StoreReferralLinks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreReferralLinks_Token",
                table: "StoreReferralLinks",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreSubscriptions_Users_ReferredByUserId",
                table: "StoreSubscriptions",
                column: "ReferredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreSubscriptions_Users_ReferredByUserId",
                table: "StoreSubscriptions");

            migrationBuilder.DropTable(
                name: "StoreReferralLinks");

            migrationBuilder.DropIndex(
                name: "IX_StoreSubscriptions_ReferredByUserId",
                table: "StoreSubscriptions");

            migrationBuilder.DropColumn(
                name: "ReferredByUserId",
                table: "StoreSubscriptions");
        }
    }
}
