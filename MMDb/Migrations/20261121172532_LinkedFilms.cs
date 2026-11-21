using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class LinkedFilms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedToFilmId",
                table: "FilmScores",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FilmScores_LinkedToFilmId",
                table: "FilmScores",
                column: "LinkedToFilmId");

            migrationBuilder.AddForeignKey(
                name: "FK_FilmScores_Films_LinkedToFilmId",
                table: "FilmScores",
                column: "LinkedToFilmId",
                principalTable: "Films",
                principalColumn: "FilmId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FilmScores_Films_LinkedToFilmId",
                table: "FilmScores");

            migrationBuilder.DropIndex(
                name: "IX_FilmScores_LinkedToFilmId",
                table: "FilmScores");

            migrationBuilder.DropColumn(
                name: "LinkedToFilmId",
                table: "FilmScores");
        }
    }
}
