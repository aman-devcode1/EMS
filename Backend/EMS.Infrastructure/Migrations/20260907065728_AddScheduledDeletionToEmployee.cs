using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledDeletionToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledForDeletionAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ScheduledForDeletionAt",
                table: "Employees",
                column: "ScheduledForDeletionAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_ScheduledForDeletionAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ScheduledForDeletionAt",
                table: "Employees");
        }
    }
}
