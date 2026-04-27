using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sgr.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMergeCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MergeCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointAId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointBId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointACreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointBCreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointACreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PointBCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DistanceMeters = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionStrategy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeCandidates", x => x.Id);
                    table.CheckConstraint("CK_MergeCandidates_DistinctPoints", "[PointAId] <> [PointBId]");
                    table.CheckConstraint("CK_MergeCandidates_Status", "[Status] IN ('pendiente','fusionado','mantenido_separado')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MergeCandidates_PointAId_PointBId",
                table: "MergeCandidates",
                columns: new[] { "PointAId", "PointBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MergeCandidates_SurveyId",
                table: "MergeCandidates",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeCandidates_SurveyId_Status",
                table: "MergeCandidates",
                columns: new[] { "SurveyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MergeCandidates");
        }
    }
}
