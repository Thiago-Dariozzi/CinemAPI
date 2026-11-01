using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShowtimeScreenStartTimeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Showtimes_ScreenId",
                table: "Showtimes");

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_ScreenId_StartTime",
                table: "Showtimes",
                columns: new[] { "ScreenId", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Showtimes_ScreenId_StartTime",
                table: "Showtimes");

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_ScreenId",
                table: "Showtimes",
                column: "ScreenId");
        }
    }
}
