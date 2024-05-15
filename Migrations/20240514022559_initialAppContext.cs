using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CandidApply.Migrations
{
    /// <inheritdoc />
    public partial class initialAppContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationStatus",
                columns: table => new
                {
                    statusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    statusName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatus", x => x.statusId);
                });

            migrationBuilder.CreateTable(
                name: "Application",
                columns: table => new
                {
                    applicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    jobTitle = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    company = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    applicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    createDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleteFlag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Application", x => x.applicationId);
                    table.ForeignKey(
                        name: "FK_Application_ApplicationStatus_status",
                        column: x => x.status,
                        principalTable: "ApplicationStatus",
                        principalColumn: "statusId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationFile",
                columns: table => new
                {
                    fileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    applicationId = table.Column<int>(type: "int", nullable: false),
                    resume = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resumePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    coverLetter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    coverLetterPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationFile", x => x.fileId);
                    table.ForeignKey(
                        name: "FK_ApplicationFile_Application_applicationId",
                        column: x => x.applicationId,
                        principalTable: "Application",
                        principalColumn: "applicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interview",
                columns: table => new
                {
                    interviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    applicationId = table.Column<int>(type: "int", nullable: false),
                    interviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    location = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    memo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interview", x => x.interviewId);
                    table.ForeignKey(
                        name: "FK_Interview_Application_applicationId",
                        column: x => x.applicationId,
                        principalTable: "Application",
                        principalColumn: "applicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Application_status",
                table: "Application",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFile_applicationId",
                table: "ApplicationFile",
                column: "applicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Interview_applicationId",
                table: "Interview",
                column: "applicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationFile");

            migrationBuilder.DropTable(
                name: "Interview");

            migrationBuilder.DropTable(
                name: "Application");

            migrationBuilder.DropTable(
                name: "ApplicationStatus");
        }
    }
}
