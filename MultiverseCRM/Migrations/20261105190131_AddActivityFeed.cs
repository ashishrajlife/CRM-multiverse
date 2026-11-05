using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiverseCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Entity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ActorUserName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityReads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastReadEventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityReads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_CreatedUtc",
                table: "ActivityEvents",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReads_UserName",
                table: "ActivityReads",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEvents");

            migrationBuilder.DropTable(
                name: "ActivityReads");
        }
    }
}
