using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sgr.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConflicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FieldKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptedValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conflicts", x => x.Id);
                    table.CheckConstraint("CK_Conflicts_Status", "[Status] IN ('pendiente','resuelto_revertido','resuelto_sin_cambio')");
                    table.CheckConstraint("CK_Conflicts_Type", "[Type] IN ('lww','owner_precedence','post_close')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conflicts_EventId",
                table: "Conflicts",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Conflicts_SurveyId",
                table: "Conflicts",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Conflicts_SurveyId_Status",
                table: "Conflicts",
                columns: new[] { "SurveyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conflicts");
        }
    }
}
