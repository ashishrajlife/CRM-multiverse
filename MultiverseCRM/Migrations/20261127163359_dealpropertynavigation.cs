using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiverseCRM.Migrations
{
    /// <inheritdoc />
    public partial class dealpropertynavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Deals_DealId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_DealId",
                table: "Properties");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Deals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyEntityId",
                table: "Deals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Deals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId1",
                table: "Deals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deals_ProjectId",
                table: "Deals",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_PropertyEntityId",
                table: "Deals",
                column: "PropertyEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_PropertyId1",
                table: "Deals",
                column: "PropertyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_Projects_ProjectId",
                table: "Deals",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_Properties_PropertyEntityId",
                table: "Deals",
                column: "PropertyEntityId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_Properties_PropertyId1",
                table: "Deals",
                column: "PropertyId1",
                principalTable: "Properties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deals_Projects_ProjectId",
                table: "Deals");

            migrationBuilder.DropForeignKey(
                name: "FK_Deals_Properties_PropertyEntityId",
                table: "Deals");

            migrationBuilder.DropForeignKey(
                name: "FK_Deals_Properties_PropertyId1",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_ProjectId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_PropertyEntityId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_PropertyId1",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "PropertyEntityId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "PropertyId1",
                table: "Deals");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_DealId",
                table: "Properties",
                column: "DealId",
                unique: true,
                filter: "[DealId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Deals_DealId",
                table: "Properties",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");
        }
    }
}
