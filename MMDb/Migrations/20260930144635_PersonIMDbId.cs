using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class PersonIMDbId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IMDbId",
                table: "People",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IMDbId",
                table: "People");
        }
    }
}
