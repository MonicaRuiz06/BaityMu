using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaityMu.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Granjas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LitrosPorVaca = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Granjas", x => x.Id);
                    table.CheckConstraint("CK_Granjas_LitrosPorVaca", "[LitrosPorVaca] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Vacas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GranjaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Raza = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaIngreso = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacas_Granjas_GranjaId",
                        column: x => x.GranjaId,
                        principalTable: "Granjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacas_GranjaId_Activa",
                table: "Vacas",
                columns: new[] { "GranjaId", "Activa" });

            migrationBuilder.CreateIndex(
                name: "IX_Vacas_GranjaId_Nombre",
                table: "Vacas",
                columns: new[] { "GranjaId", "Nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vacas");

            migrationBuilder.DropTable(
                name: "Granjas");
        }
    }
}
