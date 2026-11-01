using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieIsShowing : Migration
    {
        /// <inheritdoc />
        // defaultValue true (editado a mano antes de aplicarla): las películas existentes quedan
        // "en cartelera", que es lo que el front mostraba hasta ahora.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShowing",
                table: "Movies",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsShowing",
                table: "Movies");
        }
    }
}
