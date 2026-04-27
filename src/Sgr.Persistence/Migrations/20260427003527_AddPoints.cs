using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sgr.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Points",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    AccuracyM = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CaptureMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Points", x => x.Id);
                    table.CheckConstraint("CK_Points_CaptureMode", "[CaptureMode] IN ('detenido','movil','web')");
                    table.CheckConstraint("CK_Points_Latitude", "[Latitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_Points_Longitude", "[Longitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_Points_Origin", "[Origin] IN ('mobile_capture','mobile_edit','web_edit','web_manual_upload')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Points_SurveyId",
                table: "Points",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Points_SurveyId_CreatedBy",
                table: "Points",
                columns: new[] { "SurveyId", "CreatedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_Points_UpdatedAt",
                table: "Points",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Points");
        }
    }
}
