using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoLProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChampionRiotKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Champions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiotKey",
                table: "Champions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "RiotKey",
                table: "Champions");
        }
    }
}
