using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class OMDbCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OMDbCache",
                columns: table => new
                {
                    IMDbId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    IMDbRating = table.Column<double>(type: "double precision", nullable: true),
                    IMDbVotes = table.Column<int>(type: "integer", nullable: true),
                    RottenTomatoes = table.Column<int>(type: "integer", nullable: true),
                    Metacritic = table.Column<int>(type: "integer", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OMDbCache", x => x.IMDbId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OMDbCache");
        }
    }
}
