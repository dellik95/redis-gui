using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedisGUI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserNameAndPasswordColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Connections",
                newName: "Credentials_UserName");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Connections",
                newName: "Credentials_PasswordHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Credentials_UserName",
                table: "Connections",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "Credentials_PasswordHash",
                table: "Connections",
                newName: "PasswordHash");
        }
    }
}
