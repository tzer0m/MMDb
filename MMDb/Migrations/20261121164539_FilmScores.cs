using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class FilmScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilmComparisons",
                columns: table => new
                {
                    FilmComparisonId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilmId = table.Column<int>(type: "integer", nullable: false),
                    OtherFilmId = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilmComparisons", x => x.FilmComparisonId);
                    table.ForeignKey(
                        name: "FK_FilmComparisons_Films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "Films",
                        principalColumn: "FilmId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FilmComparisons_Films_OtherFilmId",
                        column: x => x.OtherFilmId,
                        principalTable: "Films",
                        principalColumn: "FilmId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilmScores",
                columns: table => new
                {
                    FilmId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    IsSettled = table.Column<bool>(type: "boolean", nullable: false),
                    IsAnchor = table.Column<bool>(type: "boolean", nullable: false),
                    ResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilmScores", x => x.FilmId);
                    table.CheckConstraint("CK_FilmScores_Score", "\"Score\" > 0 AND \"Score\" <= 100");
                    table.ForeignKey(
                        name: "FK_FilmScores_Films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "Films",
                        principalColumn: "FilmId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilmComparisons_FilmId_CreatedAt",
                table: "FilmComparisons",
                columns: new[] { "FilmId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FilmComparisons_OtherFilmId",
                table: "FilmComparisons",
                column: "OtherFilmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilmComparisons");

            migrationBuilder.DropTable(
                name: "FilmScores");
        }
    }
}
