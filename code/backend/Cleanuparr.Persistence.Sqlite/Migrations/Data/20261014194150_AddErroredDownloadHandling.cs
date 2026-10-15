using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleanuparr.Persistence.Migrations.Data
{
    /// <inheritdoc />
    public partial class AddErroredDownloadHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "errored_change_category",
                table: "queue_cleaner_configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "errored_delete_private",
                table: "queue_cleaner_configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "errored_ignore_private",
                table: "queue_cleaner_configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<ushort>(
                name: "errored_max_strikes",
                table: "queue_cleaner_configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: (ushort)0);

            migrationBuilder.AddColumn<bool>(
                name: "on_errored_strike",
                table: "notification_configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "errored_change_category",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "errored_delete_private",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "errored_ignore_private",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "errored_max_strikes",
                table: "queue_cleaner_configs");

            migrationBuilder.DropColumn(
                name: "on_errored_strike",
                table: "notification_configs");
        }
    }
}
