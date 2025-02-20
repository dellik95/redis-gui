using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedisGUI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewColumnInConnectionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "RedisConnectionWithCredentials",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "AnonymousRedisConnection",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "RedisConnectionWithCredentials");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "AnonymousRedisConnection");
        }
    }
}
