using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Review",
                table: "Films");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Review",
                table: "Films",
                type: "text",
                nullable: true);
        }
    }
}
