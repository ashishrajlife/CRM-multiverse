using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiverseCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertiesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    PropertyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BHK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Furnished = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Facing = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FloorNumber = table.Column<int>(type: "int", nullable: true),
                    TotalFloors = table.Column<int>(type: "int", nullable: true),
                    CarpetArea = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BuiltUpArea = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SuperBuiltUpArea = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AreaUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PricePerSqft = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OwnerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoldDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookingLeadId = table.Column<int>(type: "int", nullable: true),
                    DealId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HeroImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PropertyImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsHero = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyImages_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_City",
                table: "Properties",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_IsActive_CreatedAt",
                table: "Properties",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_IsActive_Status",
                table: "Properties",
                columns: new[] { "IsActive", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ProjectId",
                table: "Properties",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ProjectId_UnitNumber",
                table: "Properties",
                columns: new[] { "ProjectId", "UnitNumber" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Status",
                table: "Properties",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_UnitNumber",
                table: "Properties",
                column: "UnitNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyImages_PropertyId_DisplayOrder",
                table: "PropertyImages",
                columns: new[] { "PropertyId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyImages_PropertyId_IsHero",
                table: "PropertyImages",
                columns: new[] { "PropertyId", "IsHero" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyImages");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
