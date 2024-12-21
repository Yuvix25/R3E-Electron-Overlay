using Microsoft.EntityFrameworkCore.Migrations;
using ReHUD.Models;

#nullable disable

namespace ReHUD.Migrations
{
    /// <inheritdoc />
    public partial class PointGapChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Value_PointsGap",
                table: "BestLaps",
                type: "INTEGER",
                nullable: false,
                defaultValue: Driver.DATA_POINTS_GAP);

            migrationBuilder.DropColumn(
                name: "Value_PointsPerMeter",
                table: "BestLaps");

            migrationBuilder.AlterColumn<int>(
                name: "ClassPerformanceIndex",
                table: "LapContexts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value_PointsGap",
                table: "BestLaps");

            migrationBuilder.AlterColumn<int>(
                name: "ClassPerformanceIndex",
                table: "LapContexts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Value_PointsPerMeter",
                table: "BestLaps",
                type: "REAL",
                nullable: false,
                defaultValue: 0.5);
        }
    }
}
