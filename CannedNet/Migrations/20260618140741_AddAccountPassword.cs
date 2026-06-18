using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CannedNet.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "accounts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "accounts");
        }
    }
}
