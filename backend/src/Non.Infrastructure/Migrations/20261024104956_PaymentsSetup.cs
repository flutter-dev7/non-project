using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Non.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ReversalOfPaymentId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReversalOfPaymentId",
                table: "Payments",
                column: "ReversalOfPaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ReversalOfPaymentId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReversalOfPaymentId",
                table: "Payments",
                column: "ReversalOfPaymentId");
        }
    }
}
