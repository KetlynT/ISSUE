using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraficaModerna.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCpfCnpjToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CpfCnpj",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpfCnpj",
                table: "AspNetUsers");
        }
    }
}
