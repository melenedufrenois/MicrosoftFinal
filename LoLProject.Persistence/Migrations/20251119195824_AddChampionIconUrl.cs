using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoLProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChampionIconUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "Champions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "Champions");
        }
    }
}
