using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectionNameForMultiServerSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_procedure_db_proc",
                table: "tb_procedure");

            migrationBuilder.AddColumn<string>(
                name: "connection_name",
                table: "tb_procedure_aud",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "connection_name",
                table: "tb_procedure",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "DefaultConnection");

            migrationBuilder.AddColumn<string>(
                name: "connection_name",
                table: "tb_execution_log",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "DefaultConnection");

            migrationBuilder.CreateIndex(
                name: "uq_procedure_db_proc",
                table: "tb_procedure",
                columns: new[] { "connection_name", "database_name", "procedure_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_procedure_db_proc",
                table: "tb_procedure");

            migrationBuilder.DropColumn(
                name: "connection_name",
                table: "tb_procedure_aud");

            migrationBuilder.DropColumn(
                name: "connection_name",
                table: "tb_procedure");

            migrationBuilder.DropColumn(
                name: "connection_name",
                table: "tb_execution_log");

            migrationBuilder.CreateIndex(
                name: "uq_procedure_db_proc",
                table: "tb_procedure",
                columns: new[] { "database_name", "procedure_name" },
                unique: true);
        }
    }
}
