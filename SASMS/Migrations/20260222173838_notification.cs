using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASMS.Migrations
{
    /// <inheritdoc />
    public partial class notification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnActivityRegistration",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnAttendance",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnFees",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnNewActivity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnResetRequests",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnSuggestions",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyOnActivityRegistration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyOnAttendance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyOnFees",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyOnNewActivity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyOnResetRequests",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyOnSuggestions",
                table: "Users");
        }
    }
}
