using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryWebLoL.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliationId",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Affiliates",
                columns: table => new
                {
                    AffiliationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Affiliates", x => x.AffiliationId);
                    table.ForeignKey(
                        name: "FK_Affiliates_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateWarehouses",
                columns: table => new
                {
                    AffiliationId = table.Column<int>(type: "int", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateWarehouses", x => new { x.AffiliationId, x.WarehouseLocationId });
                    table.ForeignKey(
                        name: "FK_AffiliateWarehouses_Affiliates_AffiliationId",
                        column: x => x.AffiliationId,
                        principalTable: "Affiliates",
                        principalColumn: "AffiliationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffiliateWarehouses_Locations_WarehouseLocationId",
                        column: x => x.WarehouseLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Affiliates_LocationId",
                table: "Affiliates",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateWarehouses_WarehouseLocationId",
                table: "AffiliateWarehouses",
                column: "WarehouseLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffiliateWarehouses");

            migrationBuilder.DropTable(
                name: "Affiliates");

            migrationBuilder.DropColumn(
                name: "AffiliationId",
                table: "Users");
        }
    }
}
