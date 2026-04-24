using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKeysAlertsSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_incidents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    observed_value = table.Column<double>(type: "double precision", nullable: false),
                    triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_incidents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    metric_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    @operator = table.Column<int>(name: "operator", type: "integer", nullable: false),
                    threshold = table.Column<double>(type: "double precision", nullable: false),
                    window_minutes = table.Column<int>(type: "integer", nullable: false),
                    aggregation = table.Column<int>(type: "integer", nullable: false),
                    webhook_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    script_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    parameters_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_executions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_incidents_rule_id_state",
                table: "alert_incidents",
                columns: new[] { "rule_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_alert_incidents_triggered_at",
                table: "alert_incidents",
                column: "triggered_at");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_key_hash",
                table: "api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_executions_is_active_next_run_at",
                table: "scheduled_executions",
                columns: new[] { "is_active", "next_run_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_incidents");

            migrationBuilder.DropTable(
                name: "alert_rules");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "scheduled_executions");
        }
    }
}
