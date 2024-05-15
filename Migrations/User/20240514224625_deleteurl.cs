using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CandidApply.Migrations.User
{
    /// <inheritdoc />
    public partial class deleteurl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "coverLetterUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "resumeUrl",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "coverLetterUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resumeUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
