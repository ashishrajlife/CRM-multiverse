using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiverseCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertiesModulee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadId",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_DealId",
                table: "Properties",
                column: "DealId",
                unique: true,
                filter: "[DealId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_LeadId",
                table: "Properties",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Deals_DealId",
                table: "Properties",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Leads_LeadId",
                table: "Properties",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Deals_DealId",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Leads_LeadId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_DealId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_LeadId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "LeadId",
                table: "Properties");
        }
    }
}
