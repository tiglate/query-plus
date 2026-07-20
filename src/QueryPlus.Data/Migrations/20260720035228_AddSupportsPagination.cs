using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportsPagination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "supports_pagination",
                table: "tb_procedure_aud",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "supports_pagination",
                table: "tb_procedure",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supports_pagination",
                table: "tb_procedure_aud");

            migrationBuilder.DropColumn(
                name: "supports_pagination",
                table: "tb_procedure");
        }
    }
}
