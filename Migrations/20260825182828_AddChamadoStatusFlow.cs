using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddChamadoStatusFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Chamados",
                type: "TEXT",
                nullable: false,
                defaultValue: "Aberto",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Chamados",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
