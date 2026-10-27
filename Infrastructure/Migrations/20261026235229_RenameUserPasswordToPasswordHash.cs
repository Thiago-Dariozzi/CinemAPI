using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserPasswordToPasswordHash : Migration
    {
        /// <inheritdoc />
        // Editada a mano: EF proponía borrar Password y crear PasswordHash vacía, y se perdían las
        // contraseñas. Se renombra la columna conservando los valores (todavía en texto plano) y
        // DataInitializer los reemplaza por su hash al arrancar la API: el hash (PBKDF2) se calcula
        // en C#, no se puede hacer en SQL.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        // Vuelve el nombre y el tipo; los valores quedan hasheados (no se puede revertir un hash).
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Users",
                newName: "Password");
        }
    }
}
