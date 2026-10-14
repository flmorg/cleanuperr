using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleanuparr.Persistence.Postgres.Migrations.Data
{
    /// <inheritdoc />
    public partial class AddErroredDownloadHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "errored_change_category",
                schema: "data",
                table: "queue_cleaner_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "errored_delete_private",
                schema: "data",
                table: "queue_cleaner_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "errored_ignore_private",
                schema: "data",
                table: "queue_cleaner_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "errored_max_strikes",
                schema: "data",
                table: "queue_cleaner_configs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "on_errored_strike",
                schema: "data",
                table: "notification_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "errored_change_category",
                schema: "data",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "errored_delete_private",
                schema: "data",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "errored_ignore_private",
                schema: "data",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "errored_max_strikes",
                schema: "data",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "on_errored_strike",
                schema: "data",
                table: "notification_configs");
        }
    }
}
