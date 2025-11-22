using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoLProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChampionStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChampionStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    Hp = table.Column<double>(type: "float", nullable: false),
                    HpPerLevel = table.Column<double>(type: "float", nullable: false),
                    Mp = table.Column<double>(type: "float", nullable: false),
                    MpPerLevel = table.Column<double>(type: "float", nullable: false),
                    MoveSpeed = table.Column<double>(type: "float", nullable: false),
                    Armor = table.Column<double>(type: "float", nullable: false),
                    ArmorPerLevel = table.Column<double>(type: "float", nullable: false),
                    SpellBlock = table.Column<double>(type: "float", nullable: false),
                    SpellBlockPerLevel = table.Column<double>(type: "float", nullable: false),
                    AttackRange = table.Column<double>(type: "float", nullable: false),
                    HpRegen = table.Column<double>(type: "float", nullable: false),
                    HpRegenPerLevel = table.Column<double>(type: "float", nullable: false),
                    MpRegen = table.Column<double>(type: "float", nullable: false),
                    MpRegenPerLevel = table.Column<double>(type: "float", nullable: false),
                    Crit = table.Column<double>(type: "float", nullable: false),
                    CritPerLevel = table.Column<double>(type: "float", nullable: false),
                    AttackDamage = table.Column<double>(type: "float", nullable: false),
                    AttackDamagePerLevel = table.Column<double>(type: "float", nullable: false),
                    AttackSpeedPerLevel = table.Column<double>(type: "float", nullable: false),
                    AttackSpeed = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChampionStats_Champions_ChampionId",
                        column: x => x.ChampionId,
                        principalTable: "Champions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChampionStats_ChampionId",
                table: "ChampionStats",
                column: "ChampionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChampionStats");
        }
    }
}
