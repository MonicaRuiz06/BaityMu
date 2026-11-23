using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaityMu.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParametrosProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametrosProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GranjaId = table.Column<int>(type: "int", nullable: false),
                    TipoProducto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LitrosPorUnidad = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    VidaUtilDias = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosProducto", x => x.Id);
                    table.CheckConstraint("CK_ParametrosProducto_LitrosPorUnidad", "[LitrosPorUnidad] > 0");
                    table.CheckConstraint("CK_ParametrosProducto_PrecioUnitario", "[PrecioUnitario] >= 0");
                    table.CheckConstraint("CK_ParametrosProducto_VidaUtilDias", "[VidaUtilDias] > 0");
                    table.ForeignKey(
                        name: "FK_ParametrosProducto_Granjas_GranjaId",
                        column: x => x.GranjaId,
                        principalTable: "Granjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosProducto_GranjaId_TipoProducto",
                table: "ParametrosProducto",
                columns: new[] { "GranjaId", "TipoProducto" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametrosProducto");
        }
    }
}
