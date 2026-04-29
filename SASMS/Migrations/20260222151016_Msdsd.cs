using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SASMS.Migrations
{
    /// <inheritdoc />
    public partial class Msdsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Users_ManagedById",
                table: "Activities");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CreatedById",
                table: "Activities",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Users_CreatedById",
                table: "Activities",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Users_ManagedById",
                table: "Activities",
                column: "ManagedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Users_CreatedById",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Users_ManagedById",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_CreatedById",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Activities");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Users_ManagedById",
                table: "Activities",
                column: "ManagedById",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
