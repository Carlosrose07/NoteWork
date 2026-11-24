using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteWork.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCreadoPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreadoPorId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "AspNetUsers");
        }
    }
}
