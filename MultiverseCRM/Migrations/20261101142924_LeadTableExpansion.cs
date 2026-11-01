using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiverseCRM.Migrations
{
    /// <inheritdoc />
    public partial class LeadTableExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxArea",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "Leads");

            migrationBuilder.RenameColumn(
                name: "MinPrice",
                table: "Leads",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "MinArea",
                table: "Leads",
                newName: "AreaSqft");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Leads",
                newName: "Tags");

            migrationBuilder.RenameColumn(
                name: "AssignedTo",
                table: "Leads",
                newName: "Staff");

            migrationBuilder.AddColumn<string>(
                name: "AdId",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdSet",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AltPhone",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BHK",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Campaign",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChannelPartner",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCategory",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Duplicate",
                table: "Leads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DuplicateAt",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FollowUps",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FormId",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Furnished",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LatestRemark",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListingType",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LookingFor",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageId",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimarySource",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertyType",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReengagedCount",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RemarkAssignedTo",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemarkDate",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryStaff",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SiteVisitCompleted",
                table: "Leads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecificPropertyType",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Leads",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AdSet",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AltPhone",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "BHK",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Campaign",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ChannelPartner",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "CustomerCategory",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Duplicate",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "DuplicateAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "FollowUps",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "FormId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Furnished",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LatestRemark",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ListingType",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LookingFor",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "PrimarySource",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "PropertyType",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ReengagedCount",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "RemarkAssignedTo",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "RemarkDate",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "SecondaryStaff",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "SiteVisitCompleted",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "SpecificPropertyType",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Leads");

            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "Leads",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "Staff",
                table: "Leads",
                newName: "AssignedTo");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Leads",
                newName: "MinPrice");

            migrationBuilder.RenameColumn(
                name: "AreaSqft",
                table: "Leads",
                newName: "MinArea");

            migrationBuilder.AddColumn<int>(
                name: "MaxArea",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                table: "Leads",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
