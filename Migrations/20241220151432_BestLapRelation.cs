using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReHUD.Migrations
{
    /// <inheritdoc />
    public partial class BestLapRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LapContexts_BestLapId",
                table: "LapContexts",
                column: "BestLapId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LapContexts_LapDatas_BestLapId",
                table: "LapContexts",
                column: "BestLapId",
                principalTable: "LapDatas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LapContexts_LapDatas_BestLapId",
                table: "LapContexts");

            migrationBuilder.DropIndex(
                name: "IX_LapContexts_BestLapId",
                table: "LapContexts");
        }
    }
}
