using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    // Escrita a mano sobre lo que generó EF: EF proponía renombrar Tickets.UserId a ShowtimeId y
    // Tickets.ScreenId a PurchaseId (mezclando datos) y Screens.Capacity a SeatsPerRow.
    //
    // Datos existentes:
    // - Screens: SeatsPerRow = 10 y Rows = Capacity / 10 redondeado hacia arriba (mínimo 1). Si la sala
    //   tenía más de 260 lugares se usan 26 filas (máximo) y las butacas por fila necesarias, hasta 40.
    //   La capacidad nueva puede quedar un poco mayor que la anterior (25 → 3 × 10 = 30).
    // - Tickets: un ticket viejo tenía película, sala, usuario y fecha de compra, pero no función ni butaca.
    //   1. Función: la de esa película en esa sala; si hay varias, primero las activas, después la primera
    //      que empieza desde la fecha de compra y si no la más cercana a esa fecha.
    //   2. Los tickets sin ninguna función de esa película en esa sala se BORRAN: no hay de dónde sacar un
    //      horario ni una butaca, y una función inventada aparecería en la cartelera.
    //   3. Butaca: se numeran por función en orden de compra (A1, A2, ... siguiendo filas). Si una función
    //      tiene más tickets que butacas, los que no entran también se borran.
    //   4. Compra: una por ticket (antes cada ticket era una compra individual), con el mismo Id, el
    //      usuario y la fecha del ticket, subtotal = total = precio del ticket, sin cargo y confirmada.
    //      Un ticket dado de baja genera una compra dada de baja.
    public partial class AddSeatLayoutFeesAndPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Screens: Capacity → Rows × SeatsPerRow ---
            migrationBuilder.AddColumn<int>(
                name: "Rows",
                table: "Screens",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SeatsPerRow",
                table: "Screens",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.Sql("""
                UPDATE Screens SET
                    Rows = CASE
                        WHEN Capacity <= 10 THEN 1
                        WHEN Capacity > 260 THEN 26
                        ELSE CEILING(Capacity / 10.0) END,
                    SeatsPerRow = CASE
                        WHEN Capacity <= 0 THEN 10
                        WHEN Capacity > 1040 THEN 40
                        WHEN Capacity > 260 THEN CEILING(Capacity / 26.0)
                        ELSE 10 END;
                """);

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Screens");

            // --- Fees ---
            migrationBuilder.CreateTable(
                name: "Fees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fees_IsCurrent",
                table: "Fees",
                column: "IsCurrent",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsActive] = 1");

            // --- Purchases ---
            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShowtimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FeeAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Fees_FeeId",
                        column: x => x.FeeId,
                        principalTable: "Fees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_CreatedAt",
                table: "Purchases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_FeeId",
                table: "Purchases",
                column: "FeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ShowtimeId",
                table: "Purchases",
                column: "ShowtimeId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_UserId",
                table: "Purchases",
                column: "UserId");

            // --- Tickets: columnas nuevas (nulables mientras se completan) ---
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShowtimeId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeatRow",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeatNumber",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "FinalPrice",
                table: "Tickets",
                newName: "Price");

            // 1. Función de cada ticket (ver el comentario de la clase).
            migrationBuilder.Sql("""
                UPDATE t SET ShowtimeId = (
                    SELECT TOP 1 s.Id
                    FROM Showtimes s
                    WHERE s.MovieId = t.MovieId AND s.ScreenId = t.ScreenId
                    ORDER BY s.IsActive DESC,
                             CASE WHEN s.StartTime >= t.BuyDate THEN 0 ELSE 1 END,
                             ABS(DATEDIFF_BIG(MINUTE, t.BuyDate, s.StartTime)))
                FROM Tickets t;
                """);

            // 2. Sin función posible: se borran.
            migrationBuilder.Sql("DELETE FROM Tickets WHERE ShowtimeId IS NULL;");

            // 3. Butacas en orden de compra, primero los tickets activos.
            migrationBuilder.Sql("""
                WITH Numbered AS (
                    SELECT t.SeatRow, t.SeatNumber, sc.SeatsPerRow,
                           ROW_NUMBER() OVER (PARTITION BY t.ShowtimeId ORDER BY t.IsActive DESC, t.BuyDate, t.Id) - 1 AS Position
                    FROM Tickets t
                    JOIN Showtimes s ON s.Id = t.ShowtimeId
                    JOIN Screens sc ON sc.Id = s.ScreenId)
                UPDATE Numbered SET
                    SeatRow = Position / SeatsPerRow + 1,
                    SeatNumber = Position % SeatsPerRow + 1;
                """);

            // ... y los que no entran en la sala se borran.
            migrationBuilder.Sql("""
                DELETE t
                FROM Tickets t
                JOIN Showtimes s ON s.Id = t.ShowtimeId
                JOIN Screens sc ON sc.Id = s.ScreenId
                WHERE t.SeatRow > sc.Rows;
                """);

            // 4. Una compra por ticket, con el mismo Id.
            migrationBuilder.Sql("""
                INSERT INTO Purchases (Id, UserId, ShowtimeId, CreatedAt, Subtotal, FeeId, FeeAmount, Total, Status, CancelledAt, IsActive)
                SELECT Id, UserId, ShowtimeId, BuyDate, Price, NULL, 0, Price, N'Confirmed', NULL, IsActive
                FROM Tickets;

                UPDATE Tickets SET PurchaseId = Id;
                """);

            // --- Tickets: se van las columnas viejas ---
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Movies_MovieId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Screens_ScreenId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_UserId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_MovieId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ScreenId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_UserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MovieId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ScreenId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BuyDate",
                table: "Tickets");

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShowtimeId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PurchaseId",
                table: "Tickets",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ShowtimeId_SeatRow_SeatNumber",
                table: "Tickets",
                columns: new[] { "ShowtimeId", "SeatRow", "SeatNumber" },
                unique: true,
                filter: "[IsCancelled] = 0 AND [IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Purchases_PurchaseId",
                table: "Tickets",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Showtimes_ShowtimeId",
                table: "Tickets",
                column: "ShowtimeId",
                principalTable: "Showtimes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        // Vuelve al esquema anterior conservando las entradas: cada ticket recupera película, sala, usuario
        // y fecha desde su compra y su función. Se pierden las butacas, los cargos y las cancelaciones
        // (un ticket cancelado vuelve como dado de baja).
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Purchases_PurchaseId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Showtimes_ShowtimeId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_PurchaseId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ShowtimeId_SeatRow_SeatNumber",
                table: "Tickets");

            migrationBuilder.AddColumn<Guid>(
                name: "MovieId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScreenId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BuyDate",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE t SET
                    MovieId = s.MovieId,
                    ScreenId = s.ScreenId,
                    UserId = p.UserId,
                    BuyDate = p.CreatedAt,
                    IsActive = CASE WHEN t.IsActive = 1 AND t.IsCancelled = 0 AND p.IsActive = 1 THEN 1 ELSE 0 END
                FROM Tickets t
                JOIN Purchases p ON p.Id = t.PurchaseId
                JOIN Showtimes s ON s.Id = t.ShowtimeId;
                """);

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ShowtimeId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SeatRow",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SeatNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Tickets",
                newName: "FinalPrice");

            migrationBuilder.AlterColumn<Guid>(
                name: "MovieId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ScreenId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "BuyDate",
                table: "Tickets",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_MovieId",
                table: "Tickets",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ScreenId",
                table: "Tickets",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UserId",
                table: "Tickets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Movies_MovieId",
                table: "Tickets",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Screens_ScreenId",
                table: "Tickets",
                column: "ScreenId",
                principalTable: "Screens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_UserId",
                table: "Tickets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Fees");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Screens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Screens SET Capacity = Rows * SeatsPerRow;");

            migrationBuilder.DropColumn(
                name: "Rows",
                table: "Screens");

            migrationBuilder.DropColumn(
                name: "SeatsPerRow",
                table: "Screens");
        }
    }
}
