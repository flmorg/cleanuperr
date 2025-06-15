using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations.Data
{
    /// <inheritdoc />
    public partial class InitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "apprise_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: true),
                    key = table.Column<string>(type: "TEXT", nullable: true),
                    on_failed_import_strike = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_stalled_strike = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_slow_strike = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_queue_item_deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_download_cleaned = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_category_changed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_apprise_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "arr_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    failed_import_max_strikes = table.Column<short>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_arr_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "download_cleaner_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    cron_expression = table.Column<string>(type: "TEXT", nullable: false),
                    use_advanced_scheduling = table.Column<bool>(type: "INTEGER", nullable: false),
                    delete_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    unlinked_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    unlinked_target_category = table.Column<string>(type: "TEXT", nullable: false),
                    unlinked_use_tag = table.Column<bool>(type: "INTEGER", nullable: false),
                    unlinked_ignored_root_dir = table.Column<string>(type: "TEXT", nullable: false),
                    unlinked_categories = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_download_cleaner_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "download_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    type_name = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    host = table.Column<string>(type: "TEXT", nullable: true),
                    username = table.Column<string>(type: "TEXT", nullable: true),
                    password = table.Column<string>(type: "TEXT", nullable: true),
                    url_base = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_download_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "general_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    dry_run = table.Column<bool>(type: "INTEGER", nullable: false),
                    http_max_retries = table.Column<ushort>(type: "INTEGER", nullable: false),
                    http_timeout = table.Column<ushort>(type: "INTEGER", nullable: false),
                    http_certificate_validation = table.Column<string>(type: "TEXT", nullable: false),
                    search_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    search_delay = table.Column<ushort>(type: "INTEGER", nullable: false),
                    log_level = table.Column<string>(type: "TEXT", nullable: false),
                    encryption_key = table.Column<string>(type: "TEXT", nullable: false),
                    ignored_downloads = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_general_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifiarr_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    api_key = table.Column<string>(type: "TEXT", nullable: true),
                    channel_id = table.Column<string>(type: "TEXT", nullable: true),
                    on_failed_import_strike = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_stalled_strike = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_slow_strike = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_queue_item_deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_download_cleaned = table.Column<bool>(type: "INTEGER", nullable: false),
                    on_category_changed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifiarr_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "queue_cleaner_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    cron_expression = table.Column<string>(type: "TEXT", nullable: false),
                    use_advanced_scheduling = table.Column<bool>(type: "INTEGER", nullable: false),
                    content_blocker_delete_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    content_blocker_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    content_blocker_ignore_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    content_blocker_lidarr_blocklist_path = table.Column<string>(type: "TEXT", nullable: true),
                    content_blocker_lidarr_blocklist_type = table.Column<int>(type: "INTEGER", nullable: false),
                    content_blocker_radarr_blocklist_path = table.Column<string>(type: "TEXT", nullable: true),
                    content_blocker_radarr_blocklist_type = table.Column<int>(type: "INTEGER", nullable: false),
                    content_blocker_sonarr_blocklist_path = table.Column<string>(type: "TEXT", nullable: true),
                    content_blocker_sonarr_blocklist_type = table.Column<int>(type: "INTEGER", nullable: false),
                    failed_import_delete_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    failed_import_ignore_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    failed_import_ignored_patterns = table.Column<string>(type: "TEXT", nullable: false),
                    failed_import_max_strikes = table.Column<ushort>(type: "INTEGER", nullable: false),
                    slow_delete_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    slow_ignore_above_size = table.Column<string>(type: "TEXT", nullable: false),
                    slow_ignore_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    slow_max_strikes = table.Column<ushort>(type: "INTEGER", nullable: false),
                    slow_max_time = table.Column<double>(type: "REAL", nullable: false),
                    slow_min_speed = table.Column<string>(type: "TEXT", nullable: false),
                    slow_reset_strikes_on_progress = table.Column<bool>(type: "INTEGER", nullable: false),
                    stalled_delete_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    stalled_downloading_metadata_max_strikes = table.Column<ushort>(type: "INTEGER", nullable: false),
                    stalled_ignore_private = table.Column<bool>(type: "INTEGER", nullable: false),
                    stalled_max_strikes = table.Column<ushort>(type: "INTEGER", nullable: false),
                    stalled_reset_strikes_on_progress = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queue_cleaner_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "arr_instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    arr_config_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: false),
                    api_key = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_arr_instances", x => x.id);
                    table.ForeignKey(
                        name: "fk_arr_instances_arr_configs_arr_config_id",
                        column: x => x.arr_config_id,
                        principalTable: "arr_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clean_category",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    max_ratio = table.Column<double>(type: "REAL", nullable: false),
                    min_seed_time = table.Column<double>(type: "REAL", nullable: false),
                    max_seed_time = table.Column<double>(type: "REAL", nullable: false),
                    download_cleaner_config_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clean_category", x => x.id);
                    table.ForeignKey(
                        name: "fk_clean_category_download_cleaner_configs_download_cleaner_config_id",
                        column: x => x.download_cleaner_config_id,
                        principalTable: "download_cleaner_configs",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "queue_cleaner_configs",
                columns: new[]
                {
                    "id",
                    "content_blocker_delete_private",
                    "content_blocker_enabled",
                    "content_blocker_ignore_private",
                    "content_blocker_lidarr_blocklist_path",
                    "content_blocker_lidarr_blocklist_type",
                    "content_blocker_radarr_blocklist_path",
                    "content_blocker_radarr_blocklist_type",
                    "content_blocker_sonarr_blocklist_path",
                    "content_blocker_sonarr_blocklist_type",
                    "cron_expression",
                    "enabled",
                    "failed_import_delete_private",
                    "failed_import_ignore_private",
                    "failed_import_ignored_patterns",
                    "failed_import_max_strikes",
                    "slow_delete_private",
                    "slow_ignore_above_size",
                    "slow_ignore_private",
                    "slow_max_strikes",
                    "slow_max_time",
                    "slow_min_speed",
                    "slow_reset_strikes_on_progress",
                    "stalled_delete_private",
                    "stalled_downloading_metadata_max_strikes",
                    "stalled_ignore_private",
                    "stalled_max_strikes",
                    "stalled_reset_strikes_on_progress",
                    "use_advanced_scheduling"
                },
                values: new object[]
                {
                    new Guid("098ae890-21dd-4a23-9ba8-ed5ce1ab4817"),
                    false,
                    false,
                    false,
                    null,
                    0,
                    null,
                    0,
                    null,
                    0,
                    "0 0/5 * * * ?",
                    false,
                    false,
                    false,
                    "[]",
                    (ushort)0,
                    false,
                    "",
                    false,
                    (ushort)0,
                    0.0,
                    "",
                    false,
                    false,
                    (ushort)0,
                    false,
                    (ushort)0,
                    true,
                    false
                });
            
            migrationBuilder.InsertData(
                table: "download_cleaner_configs",
                columns: new[] { "id", "cron_expression", "delete_private", "enabled", "unlinked_categories", "unlinked_enabled", "unlinked_ignored_root_dir", "unlinked_target_category", "unlinked_use_tag", "use_advanced_scheduling" },
                values: new object[] { new Guid("edb20d44-9d7b-478f-aec5-93a803c26fb4"), "0 0 * * * ?", false, false, "[]", false, "", "cleanuparr-unlinked", false, false });

            migrationBuilder.InsertData(
                table: "general_configs",
                columns: new[] { "id", "dry_run", "encryption_key", "http_certificate_validation", "http_max_retries", "http_timeout", "ignored_downloads", "log_level", "search_delay", "search_enabled" },
                values: new object[] { new Guid("1490f450-1b29-4111-ab20-8a03dbd9d366"), false, "00253fe9-6c9b-4b0e-a05e-e5d2164f2389", "Enabled", (ushort)0, (ushort)100, "[]", "Information", (ushort)30, true });

            migrationBuilder.InsertData(
                table: "notifiarr_configs",
                columns: new[] { "id", "api_key", "channel_id", "on_category_changed", "on_download_cleaned", "on_failed_import_strike", "on_queue_item_deleted", "on_slow_strike", "on_stalled_strike" },
                values: new object[] { new Guid("dd468589-e5ee-4e1b-b05e-28b461894846"), null, null, false, false, false, false, false, false });

            migrationBuilder.InsertData(
                table: "apprise_configs",
                columns: new[] { "id", "key", "on_category_changed", "on_download_cleaned", "on_failed_import_strike", "on_queue_item_deleted", "on_slow_strike", "on_stalled_strike", "url" },
                values: new object[] { new Guid("9c7a346a-2b80-4935-ae4f-5400e336fd07"), null, false, false, false, false, false, false, null });
            
            migrationBuilder.InsertData(
                table: "arr_configs",
                columns: new[] { "id", "enabled", "failed_import_max_strikes", "type" },
                values: new object[] { new Guid("6096303a-399c-42b8-be8f-60a02cec5a51"), false, (short)-1, "radarr" });
            
            migrationBuilder.InsertData(
                table: "arr_configs",
                columns: new[] { "id", "enabled", "failed_import_max_strikes", "type" },
                values: new object[] { new Guid("4fd2b82b-cffd-4b41-bcc0-204058b1e459"), false, (short)-1, "lidarr" });

            migrationBuilder.InsertData(
                table: "arr_configs",
                columns: new[] { "id", "enabled", "failed_import_max_strikes", "type" },
                values: new object[] { new Guid("0b38a68f-3d7b-4d98-ae96-115da62d9af2"), false, (short)-1, "sonarr" });

            migrationBuilder.CreateIndex(
                name: "ix_arr_instances_arr_config_id",
                table: "arr_instances",
                column: "arr_config_id");

            migrationBuilder.CreateIndex(
                name: "ix_clean_category_download_cleaner_config_id",
                table: "clean_category",
                column: "download_cleaner_config_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "apprise_configs");

            migrationBuilder.DropTable(
                name: "arr_instances");

            migrationBuilder.DropTable(
                name: "clean_category");

            migrationBuilder.DropTable(
                name: "download_clients");

            migrationBuilder.DropTable(
                name: "general_configs");

            migrationBuilder.DropTable(
                name: "notifiarr_configs");

            migrationBuilder.DropTable(
                name: "queue_cleaner_configs");

            migrationBuilder.DropTable(
                name: "arr_configs");

            migrationBuilder.DropTable(
                name: "download_cleaner_configs");
        }
    }
}
