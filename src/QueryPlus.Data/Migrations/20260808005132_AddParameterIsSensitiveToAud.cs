using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParameterIsSensitiveToAud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_sensitive",
                table: "tb_procedure_parameter_aud",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_sensitive",
                table: "tb_procedure_parameter_aud");
        }
    }
}
