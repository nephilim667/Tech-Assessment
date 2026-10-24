using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseReportNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "expense_reports",
                type: "integer",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.CreateIndex(
                name: "IX_expense_reports_Number",
                table: "expense_reports",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_expense_reports_Number",
                table: "expense_reports");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "expense_reports");
        }
    }
}
