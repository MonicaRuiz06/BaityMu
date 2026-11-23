using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaityMu.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LotesProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LotesProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoProducto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaProduccion = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    LitrosUsados = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CantidadInicial = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    CantidadDisponible = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    CantidadDanada = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesProducto", x => x.Id);
                    table.CheckConstraint("CK_LotesProducto_CantidadDanada", "[CantidadDanada] >= 0");
                    table.CheckConstraint("CK_LotesProducto_CantidadDisponible", "[CantidadDisponible] >= 0");
                    table.CheckConstraint("CK_LotesProducto_Cantidades", "[CantidadDisponible] + [CantidadDanada] <= [CantidadInicial]");
                    table.CheckConstraint("CK_LotesProducto_CantidadInicial", "[CantidadInicial] > 0");
                    table.CheckConstraint("CK_LotesProducto_FechaVencimiento", "[FechaVencimiento] >= [FechaProduccion]");
                    table.CheckConstraint("CK_LotesProducto_LitrosUsados", "[LitrosUsados] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LotesProducto_FechaProduccion",
                table: "LotesProducto",
                column: "FechaProduccion");

            migrationBuilder.CreateIndex(
                name: "IX_LotesProducto_FechaVencimiento",
                table: "LotesProducto",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_LotesProducto_FIFO",
                table: "LotesProducto",
                columns: new[] { "TipoProducto", "FechaProduccion", "Id" })
                .Annotation("SqlServer:Include", new[] { "CantidadDisponible", "FechaVencimiento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotesProducto");
        }
    }
}
