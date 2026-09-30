using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class CreditCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Character",
                table: "FilmCredits",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Character",
                table: "FilmCredits");
        }
    }
}
