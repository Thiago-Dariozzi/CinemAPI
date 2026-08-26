using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenreEntityAndMovieGenreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTA: esta migración fue retocada a mano. La que generó EF por defecto
            // borraba la columna Genre (string) ANTES de poder usarla para completar
            // GenreId, perdiendo el dato de género de cualquier película ya sembrada.
            // Acá el orden es: crear Genres -> poblarla con los valores distintos que ya
            // había en Movies.Genre -> agregar GenreId nullable -> completarla desde el
            // string viejo -> recién ahí forzarla NOT NULL y borrar la columna Genre.

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            // Un Genre por cada valor distinto que ya tuvieran las películas sembradas
            // (si la base está vacía, esto simplemente no inserta nada).
            migrationBuilder.Sql(@"
                INSERT INTO Genres (Id, Name, IsActive)
                SELECT NEWID(), DistinctGenres.Genre, 1
                FROM (SELECT DISTINCT Genre FROM Movies WHERE Genre IS NOT NULL AND Genre <> '') AS DistinctGenres;
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "GenreId",
                table: "Movies",
                type: "uniqueidentifier",
                nullable: true);

            // Completa GenreId a partir del string viejo, matcheando por nombre.
            migrationBuilder.Sql(@"
                UPDATE m
                SET m.GenreId = g.Id
                FROM Movies m
                INNER JOIN Genres g ON g.Name = m.Genre;
            ");

            // Red de seguridad: si alguna película quedó sin poder matchear (no debería
            // pasar), le asignamos cualquier género existente para no dejar FKs nulas
            // antes de forzar la columna a NOT NULL.
            migrationBuilder.Sql(@"
                UPDATE Movies
                SET GenreId = (SELECT TOP 1 Id FROM Genres)
                WHERE GenreId IS NULL AND EXISTS (SELECT 1 FROM Genres);
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "GenreId",
                table: "Movies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Movies");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_GenreId",
                table: "Movies",
                column: "GenreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_Genres_GenreId",
                table: "Movies",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movies_Genres_GenreId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_GenreId",
                table: "Movies");

            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Movies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Recupera el string viejo a partir del nombre del género antes de borrar la FK.
            migrationBuilder.Sql(@"
                UPDATE m
                SET m.Genre = g.Name
                FROM Movies m
                INNER JOIN Genres g ON g.Id = m.GenreId;
            ");

            migrationBuilder.DropColumn(
                name: "GenreId",
                table: "Movies");

            migrationBuilder.DropTable(
                name: "Genres");
        }
    }
}
