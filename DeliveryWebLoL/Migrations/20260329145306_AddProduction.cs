using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryWebLoL.Migrations
{
    /// <inheritdoc />
    public partial class AddProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_LocationID",
                table: "Inventories");

            migrationBuilder.CreateTable(
                name: "LocationItemProductions",
                columns: table => new
                {
                    LocationID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitsPerMinute = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationItemProductions", x => new { x.LocationID, x.ItemID });
                    table.ForeignKey(
                        name: "FK_LocationItemProductions_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationItemProductions_Locations_LocationID",
                        column: x => x.LocationID,
                        principalTable: "Locations",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_LocationID_ItemID",
                table: "Inventories",
                columns: new[] { "LocationID", "ItemID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationItemProductions_ItemID",
                table: "LocationItemProductions",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_LocationItemProductions_LocationID",
                table: "LocationItemProductions",
                column: "LocationID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationItemProductions");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_LocationID_ItemID",
                table: "Inventories");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_LocationID",
                table: "Inventories",
                column: "LocationID");
        }
    }
}
