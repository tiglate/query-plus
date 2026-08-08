using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionStartIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_execution_log_date",
                table: "tb_execution_log",
                column: "execution_start",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_execution_log_date",
                table: "tb_execution_log");
        }
    }
}
