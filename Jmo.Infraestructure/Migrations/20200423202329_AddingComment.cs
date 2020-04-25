using Microsoft.EntityFrameworkCore.Migrations;

namespace Jmo.Infraestructure.Migrations
{
    public partial class AddingComment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerRestrospective",
                table: "Questions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerRestrospective",
                table: "Questions");
        }
    }
}
