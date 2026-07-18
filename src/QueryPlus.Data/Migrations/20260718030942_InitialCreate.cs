using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QueryPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_category",
                columns: table => new
                {
                    id_category = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    description = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.id_category);
                });

            migrationBuilder.CreateTable(
                name: "tb_revision",
                columns: table => new
                {
                    id_revision = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    revision_timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    username = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ip_address = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revision", x => x.id_revision);
                });

            migrationBuilder.CreateTable(
                name: "tb_revision_type",
                columns: table => new
                {
                    id_revision_type = table.Column<byte>(type: "tinyint", nullable: false),
                    description = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revision_type", x => x.id_revision_type);
                });

            migrationBuilder.CreateTable(
                name: "tb_procedure",
                columns: table => new
                {
                    id_procedure = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_category = table.Column<int>(type: "int", nullable: false),
                    caption = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    database_name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    procedure_name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    role_entitlement = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure", x => x.id_procedure);
                    table.ForeignKey(
                        name: "fk_procedure_category",
                        column: x => x.id_category,
                        principalTable: "tb_category",
                        principalColumn: "id_category",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_category_aud",
                columns: table => new
                {
                    id_category = table.Column<int>(type: "int", nullable: false),
                    id_revision = table.Column<int>(type: "int", nullable: false),
                    id_revision_type = table.Column<byte>(type: "tinyint", nullable: true),
                    description = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_aud", x => new { x.id_category, x.id_revision });
                    table.ForeignKey(
                        name: "fk_category_aud_revision",
                        column: x => x.id_revision,
                        principalTable: "tb_revision",
                        principalColumn: "id_revision",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_category_aud_revision_type",
                        column: x => x.id_revision_type,
                        principalTable: "tb_revision_type",
                        principalColumn: "id_revision_type",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_procedure_aud",
                columns: table => new
                {
                    id_procedure = table.Column<int>(type: "int", nullable: false),
                    id_revision = table.Column<int>(type: "int", nullable: false),
                    id_revision_type = table.Column<byte>(type: "tinyint", nullable: true),
                    id_category = table.Column<int>(type: "int", nullable: true),
                    caption = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    database_name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    procedure_name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: true),
                    role_entitlement = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_aud", x => new { x.id_procedure, x.id_revision });
                    table.ForeignKey(
                        name: "fk_procedure_aud_revision",
                        column: x => x.id_revision,
                        principalTable: "tb_revision",
                        principalColumn: "id_revision",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_procedure_aud_revision_type",
                        column: x => x.id_revision_type,
                        principalTable: "tb_revision_type",
                        principalColumn: "id_revision_type",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_procedure_column_aud",
                columns: table => new
                {
                    id_procedure_column = table.Column<int>(type: "int", nullable: false),
                    id_revision = table.Column<int>(type: "int", nullable: false),
                    id_revision_type = table.Column<byte>(type: "tinyint", nullable: true),
                    id_procedure = table.Column<int>(type: "int", nullable: true),
                    technical_name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    caption = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    alignment = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    format_mask = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    visible = table.Column<bool>(type: "bit", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_column_aud", x => new { x.id_procedure_column, x.id_revision });
                    table.ForeignKey(
                        name: "fk_column_aud_revision",
                        column: x => x.id_revision,
                        principalTable: "tb_revision",
                        principalColumn: "id_revision",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_column_aud_revision_type",
                        column: x => x.id_revision_type,
                        principalTable: "tb_revision_type",
                        principalColumn: "id_revision_type",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_procedure_parameter_aud",
                columns: table => new
                {
                    id_procedure_parameter = table.Column<int>(type: "int", nullable: false),
                    id_revision = table.Column<int>(type: "int", nullable: false),
                    id_revision_type = table.Column<byte>(type: "tinyint", nullable: true),
                    id_procedure = table.Column<int>(type: "int", nullable: true),
                    caption = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    parameter_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    default_value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    combo_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parameter_aud", x => new { x.id_procedure_parameter, x.id_revision });
                    table.ForeignKey(
                        name: "fk_parameter_aud_revision",
                        column: x => x.id_revision,
                        principalTable: "tb_revision",
                        principalColumn: "id_revision",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_parameter_aud_revision_type",
                        column: x => x.id_revision_type,
                        principalTable: "tb_revision_type",
                        principalColumn: "id_revision_type",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_execution_log",
                columns: table => new
                {
                    id_execution_log = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_procedure = table.Column<int>(type: "int", nullable: false),
                    username = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ip_address = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    execution_start = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    execution_end = table.Column<DateTime>(type: "datetime2", nullable: true),
                    success = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    parameter_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    row_count = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_execution_log", x => x.id_execution_log);
                    table.ForeignKey(
                        name: "fk_log_procedure",
                        column: x => x.id_procedure,
                        principalTable: "tb_procedure",
                        principalColumn: "id_procedure",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_procedure_column",
                columns: table => new
                {
                    id_procedure_column = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_procedure = table.Column<int>(type: "int", nullable: false),
                    technical_name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    caption = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    alignment = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "Left"),
                    format_mask = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    visible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_column", x => x.id_procedure_column);
                    table.ForeignKey(
                        name: "fk_column_procedure",
                        column: x => x.id_procedure,
                        principalTable: "tb_procedure",
                        principalColumn: "id_procedure",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_procedure_parameter",
                columns: table => new
                {
                    id_procedure_parameter = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_procedure = table.Column<int>(type: "int", nullable: false),
                    caption = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    parameter_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    default_value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    combo_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_parameter", x => x.id_procedure_parameter);
                    table.ForeignKey(
                        name: "fk_parameter_procedure",
                        column: x => x.id_procedure,
                        principalTable: "tb_procedure",
                        principalColumn: "id_procedure",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tb_revision_type",
                columns: new[] { "id_revision_type", "description" },
                values: new object[,]
                {
                    { (byte)1, "INSERT" },
                    { (byte)2, "UPDATE" },
                    { (byte)3, "DELETE" }
                });

            migrationBuilder.CreateIndex(
                name: "uq_category_description",
                table: "tb_category",
                column: "description",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_category_aud_id_revision",
                table: "tb_category_aud",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "IX_tb_category_aud_id_revision_type",
                table: "tb_category_aud",
                column: "id_revision_type");

            migrationBuilder.CreateIndex(
                name: "ix_execution_log_proc_date",
                table: "tb_execution_log",
                columns: new[] { "id_procedure", "execution_start" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_execution_log_user_date",
                table: "tb_execution_log",
                columns: new[] { "username", "execution_start" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_tb_procedure_id_category",
                table: "tb_procedure",
                column: "id_category");

            migrationBuilder.CreateIndex(
                name: "uq_procedure_caption",
                table: "tb_procedure",
                column: "caption",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_procedure_db_proc",
                table: "tb_procedure",
                columns: new[] { "database_name", "procedure_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_procedure_aud_id_revision",
                table: "tb_procedure_aud",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "IX_tb_procedure_aud_id_revision_type",
                table: "tb_procedure_aud",
                column: "id_revision_type");

            migrationBuilder.CreateIndex(
                name: "uq_column_procedure_tech",
                table: "tb_procedure_column",
                columns: new[] { "id_procedure", "technical_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_procedure_column_aud_id_revision",
                table: "tb_procedure_column_aud",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "IX_tb_procedure_column_aud_id_revision_type",
                table: "tb_procedure_column_aud",
                column: "id_revision_type");

            migrationBuilder.CreateIndex(
                name: "uq_parameter_procedure_name",
                table: "tb_procedure_parameter",
                columns: new[] { "id_procedure", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_procedure_parameter_aud_id_revision",
                table: "tb_procedure_parameter_aud",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "IX_tb_procedure_parameter_aud_id_revision_type",
                table: "tb_procedure_parameter_aud",
                column: "id_revision_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_category_aud");

            migrationBuilder.DropTable(
                name: "tb_execution_log");

            migrationBuilder.DropTable(
                name: "tb_procedure_aud");

            migrationBuilder.DropTable(
                name: "tb_procedure_column");

            migrationBuilder.DropTable(
                name: "tb_procedure_column_aud");

            migrationBuilder.DropTable(
                name: "tb_procedure_parameter");

            migrationBuilder.DropTable(
                name: "tb_procedure_parameter_aud");

            migrationBuilder.DropTable(
                name: "tb_procedure");

            migrationBuilder.DropTable(
                name: "tb_revision");

            migrationBuilder.DropTable(
                name: "tb_revision_type");

            migrationBuilder.DropTable(
                name: "tb_category");
        }
    }
}
