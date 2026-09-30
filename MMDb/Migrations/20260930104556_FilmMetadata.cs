using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class FilmMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackdropPath",
                table: "Films",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Cast",
                table: "Films",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Genres",
                table: "Films",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "IMDbId",
                table: "Films",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IMDbRating",
                table: "Films",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IMDbVotes",
                table: "Films",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Metacritic",
                table: "Films",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterPath",
                table: "Films",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatingsUpdatedAt",
                table: "Films",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RottenTomatoes",
                table: "Films",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TMDbId",
                table: "Films",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TMDbRating",
                table: "Films",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "Films",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Films_IMDbId",
                table: "Films",
                column: "IMDbId");

            migrationBuilder.CreateIndex(
                name: "IX_Films_TMDbId",
                table: "Films",
                column: "TMDbId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Films_IMDbId",
                table: "Films");

            migrationBuilder.DropIndex(
                name: "IX_Films_TMDbId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "BackdropPath",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "Cast",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "Genres",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "IMDbId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "IMDbRating",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "IMDbVotes",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "Metacritic",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "PosterPath",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "RatingsUpdatedAt",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "RottenTomatoes",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "TMDbId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "TMDbRating",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "Films");
        }
    }
}
