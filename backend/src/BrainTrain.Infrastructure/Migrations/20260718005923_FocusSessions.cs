using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrainTrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FocusSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FocusDateUtc",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusXpToday",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FocusDateUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FocusXpToday",
                table: "Users");
        }
    }
}
