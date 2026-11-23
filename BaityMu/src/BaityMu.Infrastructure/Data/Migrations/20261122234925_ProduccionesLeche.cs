using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaityMu.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProduccionesLeche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProduccionesLeche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Litros = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProduccionesLeche", x => x.Id);
                    table.CheckConstraint("CK_ProduccionesLeche_Litros", "[Litros] >= 0");
                    table.ForeignKey(
                        name: "FK_ProduccionesLeche_Vacas_VacaId",
                        column: x => x.VacaId,
                        principalTable: "Vacas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionesLeche_Fecha",
                table: "ProduccionesLeche",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionesLeche_VacaId_Fecha",
                table: "ProduccionesLeche",
                columns: new[] { "VacaId", "Fecha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProduccionesLeche");
        }
    }
}
