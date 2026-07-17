using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFastCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "StoreSubscriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "StoreSubscriptions");
        }
    }
}
