using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteWork.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarActivoAUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CORREGIDO a mano: EF generó defaultValue: false, y eso dejaría
            // desactivados a TODOS los usuarios que ya existen (incluido el admin),
            // que no podrían volver a entrar. Con true, los usuarios actuales
            // quedan activos, igual que el valor por defecto de Usuario.Activo.
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "AspNetUsers");
        }
    }
}