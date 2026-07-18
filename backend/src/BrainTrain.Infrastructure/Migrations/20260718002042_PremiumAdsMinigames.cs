using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrainTrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PremiumAdsMinigames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AdRewardDateUtc",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdRewardsToday",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MinigameDateUtc",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinigameXpToday",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumUntilUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdRewardDateUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdRewardsToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MinigameDateUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MinigameXpToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PremiumUntilUtc",
                table: "Users");
        }
    }
}
