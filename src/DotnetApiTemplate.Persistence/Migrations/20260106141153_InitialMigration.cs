using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotnetApiTemplate.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Make = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VIN = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    Mileage = table.Column<int>(type: "integer", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_IsAvailable",
                table: "Cars",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Make",
                table: "Cars",
                column: "Make");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Make_Model",
                table: "Cars",
                columns: new[] { "Make", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Model",
                table: "Cars",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Price",
                table: "Cars",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Year",
                table: "Cars",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cars");
        }
    }
}
