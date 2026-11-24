using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteWork.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLimitesEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Empresas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsuarios",
                table: "Empresas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "MaxUsuarios",
                table: "Empresas");
        }
    }
}
