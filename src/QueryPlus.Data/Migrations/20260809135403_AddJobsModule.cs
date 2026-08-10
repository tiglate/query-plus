using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_job_definition",
                columns: table => new
                {
                    id_job_definition = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    job_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    script_path = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false),
                    script_sha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    cron_expression = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    run_as_user = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    memory_limit_mb = table.Column<int>(type: "int", nullable: false),
                    max_duration_minutes = table.Column<int>(type: "int", nullable: false),
                    enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    approval_status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Draft"),
                    created_by = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    approved_by = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rejection_reason = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    notify_emails = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_definition", x => x.id_job_definition);
                    table.CheckConstraint("ck_job_definition_no_self_approval", "[approved_by] IS NULL OR [approved_by] <> [created_by]");
                });

            migrationBuilder.CreateTable(
                name: "tb_job_definition_aud",
                columns: table => new
                {
                    id_job_definition = table.Column<int>(type: "int", nullable: false),
                    id_revision = table.Column<int>(type: "int", nullable: false),
                    id_revision_type = table.Column<byte>(type: "tinyint", nullable: true),
                    name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    job_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    script_path = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    script_sha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    cron_expression = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true),
                    run_as_user = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    memory_limit_mb = table.Column<int>(type: "int", nullable: true),
                    max_duration_minutes = table.Column<int>(type: "int", nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: true),
                    approval_status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    approved_by = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rejection_reason = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    notify_emails = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_definition_aud", x => new { x.id_job_definition, x.id_revision });
                    table.ForeignKey(
                        name: "fk_job_definition_aud_revision",
                        column: x => x.id_revision,
                        principalTable: "tb_revision",
                        principalColumn: "id_revision",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_job_definition_aud_revision_type",
                        column: x => x.id_revision_type,
                        principalTable: "tb_revision_type",
                        principalColumn: "id_revision_type",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_job_run",
                columns: table => new
                {
                    id_job_run = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_job_definition = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    triggered_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    runner_pid = table.Column<int>(type: "int", nullable: true),
                    runner_started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    child_pid = table.Column<int>(type: "int", nullable: true),
                    child_started_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_heartbeat_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    finished_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    exit_code = table.Column<int>(type: "int", nullable: true),
                    stdout_path = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    stderr_path = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    host_machine = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_run", x => x.id_job_run);
                    table.ForeignKey(
                        name: "fk_job_run_job_definition",
                        column: x => x.id_job_definition,
                        principalTable: "tb_job_definition",
                        principalColumn: "id_job_definition",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_job_run_request",
                columns: table => new
                {
                    id_job_run_request = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_job_definition = table.Column<int>(type: "int", nullable: false),
                    requested_by = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    consumed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    id_job_run = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_run_request", x => x.id_job_run_request);
                    table.ForeignKey(
                        name: "fk_job_run_request_job_definition",
                        column: x => x.id_job_definition,
                        principalTable: "tb_job_definition",
                        principalColumn: "id_job_definition",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_job_run_request_job_run",
                        column: x => x.id_job_run,
                        principalTable: "tb_job_run",
                        principalColumn: "id_job_run",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_definition_approved_enabled",
                table: "tb_job_definition",
                columns: new[] { "approval_status", "enabled" });

            migrationBuilder.CreateIndex(
                name: "uq_job_definition_name",
                table: "tb_job_definition",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_job_definition_aud_id_revision",
                table: "tb_job_definition_aud",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "IX_tb_job_definition_aud_id_revision_type",
                table: "tb_job_definition_aud",
                column: "id_revision_type");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_job_definition_started",
                table: "tb_job_run",
                columns: new[] { "id_job_definition", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_job_run_status",
                table: "tb_job_run",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_request_pending",
                table: "tb_job_run_request",
                column: "consumed_at",
                filter: "[consumed_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_job_run_request_id_job_definition",
                table: "tb_job_run_request",
                column: "id_job_definition");

            migrationBuilder.CreateIndex(
                name: "IX_tb_job_run_request_id_job_run",
                table: "tb_job_run_request",
                column: "id_job_run",
                unique: true,
                filter: "[id_job_run] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_job_definition_aud");

            migrationBuilder.DropTable(
                name: "tb_job_run_request");

            migrationBuilder.DropTable(
                name: "tb_job_run");

            migrationBuilder.DropTable(
                name: "tb_job_definition");
        }
    }
}
