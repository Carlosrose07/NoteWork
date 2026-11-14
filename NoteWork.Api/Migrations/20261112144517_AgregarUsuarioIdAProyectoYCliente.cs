using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteWork.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsuarioIdAProyectoYCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Proyectos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Clientes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // EDITADO A MANO (B1): asigna los datos que ya existen al usuario
            // carlitoflore02@gmail.com. Incluye las filas con Eliminado = true,
            // porque este UPDATE va directo a la tabla y no usa ningún filtro.
            // Solo toca filas que quedaron con dueño vacío (ceros).
            // Si hay filas por asignar y el usuario NO existe, falla a propósito:
            // la migración se revierte y no queda nada a medias.
            // Si no hay filas (base vacía), no hace nada y no falla.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    v_id uuid;
    v_vacio uuid := '00000000-0000-0000-0000-000000000000';
BEGIN
    SELECT ""Id"" INTO v_id
    FROM ""AspNetUsers""
    WHERE ""NormalizedEmail"" = 'CARLITOFLORE02@GMAIL.COM';

    IF v_id IS NULL THEN
        IF EXISTS (SELECT 1 FROM ""Proyectos"" WHERE ""UsuarioId"" = v_vacio)
           OR EXISTS (SELECT 1 FROM ""Clientes"" WHERE ""UsuarioId"" = v_vacio) THEN
            RAISE EXCEPTION 'No existe el usuario carlitoflore02@gmail.com; no se pueden asignar los datos existentes.';
        END IF;
    ELSE
        UPDATE ""Proyectos"" SET ""UsuarioId"" = v_id WHERE ""UsuarioId"" = v_vacio;
        UPDATE ""Clientes""  SET ""UsuarioId"" = v_id WHERE ""UsuarioId"" = v_vacio;
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Clientes");
        }
    }
}