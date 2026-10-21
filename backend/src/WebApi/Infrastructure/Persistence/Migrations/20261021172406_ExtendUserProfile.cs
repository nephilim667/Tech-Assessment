using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxExpensesPerMonth",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Complète les utilisateurs de démonstration du socle initial.
            // Ces valeurs ne constituent pas une politique de migration
            // de données réelles.
            migrationBuilder.Sql("""
                UPDATE users
                SET
                    "Street" = '10 rue des Lilas',
                    "PostalCode" = '75001',
                    "City" = 'Paris',
                    "MaxExpensesPerMonth" = 5,
                    "IsActive" = TRUE
                WHERE "FirstName" = 'Juste'
                  AND "LastName" = 'Leblanc';
            
                UPDATE users
                SET
                    "Street" = '20 rue des Roses',
                    "PostalCode" = '69001',
                    "City" = 'Lyon',
                    "MaxExpensesPerMonth" = 10,
                    "IsActive" = TRUE
                WHERE "FirstName" = 'Marc'
                  AND "LastName" = 'Assin';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_MaxExpensesPerMonth",
                table: "users",
                sql: "\"MaxExpensesPerMonth\" >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_MaxExpensesPerMonth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "City",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MaxExpensesPerMonth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "users");
        }
    }
}
