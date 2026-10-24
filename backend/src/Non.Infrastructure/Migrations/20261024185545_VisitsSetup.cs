using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Non.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VisitsSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_AgentId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_ShopId",
                table: "Visits");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AgentId_VisitedAt",
                table: "Visits",
                columns: new[] { "AgentId", "VisitedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ShopId_VisitedAt",
                table: "Visits",
                columns: new[] { "ShopId", "VisitedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_AgentId_VisitedAt",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_ShopId_VisitedAt",
                table: "Visits");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AgentId",
                table: "Visits",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ShopId",
                table: "Visits",
                column: "ShopId");
        }
    }
}
