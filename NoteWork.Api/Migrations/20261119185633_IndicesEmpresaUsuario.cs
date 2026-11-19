using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteWork.Api.Migrations
{
    /// <inheritdoc />
    public partial class IndicesEmpresaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proyectos_EmpresaId",
                table: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_EmpresaId",
                table: "Clientes");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_EmpresaId_UsuarioId",
                table: "Proyectos",
                columns: new[] { "EmpresaId", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EmpresaId_UsuarioId",
                table: "Clientes",
                columns: new[] { "EmpresaId", "UsuarioId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proyectos_EmpresaId_UsuarioId",
                table: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_EmpresaId_UsuarioId",
                table: "Clientes");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_EmpresaId",
                table: "Proyectos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EmpresaId",
                table: "Clientes",
                column: "EmpresaId");
        }
    }
}
