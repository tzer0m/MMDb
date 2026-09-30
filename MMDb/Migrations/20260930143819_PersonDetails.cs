using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class PersonDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Biography",
                table: "People",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Birthday",
                table: "People",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Deathday",
                table: "People",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DetailsUpdatedAt",
                table: "People",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfBirth",
                table: "People",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Biography",
                table: "People");

            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "People");

            migrationBuilder.DropColumn(
                name: "Deathday",
                table: "People");

            migrationBuilder.DropColumn(
                name: "DetailsUpdatedAt",
                table: "People");

            migrationBuilder.DropColumn(
                name: "PlaceOfBirth",
                table: "People");
        }
    }
}
