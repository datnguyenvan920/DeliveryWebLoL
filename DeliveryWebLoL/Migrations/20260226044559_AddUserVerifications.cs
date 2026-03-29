using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryWebLoL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVerifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VerifyExpiration",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifyNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifyExpiration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerifyNumber",
                table: "Users");
        }
    }
}
