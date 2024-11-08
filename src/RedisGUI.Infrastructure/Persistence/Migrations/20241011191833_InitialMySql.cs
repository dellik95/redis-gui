using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace RedisGUI.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class InitialMySql : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterDatabase()
				.Annotation("MySQL:Charset", "utf8mb4");

			migrationBuilder.CreateTable(
				name: "Connections",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "char(36)", nullable: false),
					Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
					Host = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
					Port = table.Column<int>(type: "int", nullable: false),
					Credentials_UserName = table.Column<string>(type: "longtext", nullable: false),
					Credentials_PasswordHash = table.Column<string>(type: "longtext", nullable: false),
					Database = table.Column<int>(type: "int", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Connections", x => x.Id);
				})
				.Annotation("MySQL:Charset", "utf8mb4");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Connections");
		}
	}
}
