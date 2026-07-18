using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParameterIsRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                table: "tb_procedure_parameter_aud",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                table: "tb_procedure_parameter",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_required",
                table: "tb_procedure_parameter_aud");

            migrationBuilder.DropColumn(
                name: "is_required",
                table: "tb_procedure_parameter");
        }
    }
}
