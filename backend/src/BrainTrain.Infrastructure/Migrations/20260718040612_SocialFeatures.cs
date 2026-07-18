using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrainTrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SocialFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CalibratedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorrectToday",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LeagueTier",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinigamesToday",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OwnedAvatarsCsv",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PerfectsToday",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuestClaimedMask",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "QuestDateUtc",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionsToday",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DuelId",
                table: "GameSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Duels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ChallengerUserId = table.Column<long>(type: "bigint", nullable: false),
                    OpponentUserId = table.Column<long>(type: "bigint", nullable: true),
                    QuestionIdsCsv = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ChallengerScore = table.Column<int>(type: "integer", nullable: true),
                    OpponentScore = table.Column<int>(type: "integer", nullable: true),
                    TotalCount = table.Column<int>(type: "integer", nullable: false),
                    IsOpenToPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Duels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Duels_Users_ChallengerUserId",
                        column: x => x.ChallengerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Duels_Users_OpponentUserId",
                        column: x => x.OpponentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Duels_ChallengerUserId",
                table: "Duels",
                column: "ChallengerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Duels_Code",
                table: "Duels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Duels_IsOpenToPublic_Status",
                table: "Duels",
                columns: new[] { "IsOpenToPublic", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Duels_OpponentUserId",
                table: "Duels",
                column: "OpponentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Duels");

            migrationBuilder.DropColumn(
                name: "CalibratedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CorrectToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LeagueTier",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MinigamesToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OwnedAvatarsCsv",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PerfectsToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "QuestClaimedMask",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "QuestDateUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SessionsToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DuelId",
                table: "GameSessions");
        }
    }
}
