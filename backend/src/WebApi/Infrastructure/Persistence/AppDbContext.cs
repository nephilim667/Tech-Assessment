using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using WebApi.Infrastructure.Persistence.Entities;

namespace WebApi.Infrastructure.Persistence;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserEntity> Beneficiaries => Set<UserEntity>();
    public DbSet<ExpenseReportEntity> ExpenseReports => Set<ExpenseReportEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserEntity>()
               .ToTable("users", table => table.HasCheckConstraint("CK_users_MaxExpensesPerMonth", "\"MaxExpensesPerMonth\" >= 1"));

        builder.Entity<ExpenseReportEntity>(entity =>
        {
            entity.ToTable("expense_reports", table =>
            {
                table.HasCheckConstraint("CK_expense_reports_Year", "\"Year\" BETWEEN 1 AND 9999");
                table.HasCheckConstraint("CK_expense_reports_Month", "\"Month\" BETWEEN 1 AND 12");
            });

            entity.HasOne<UserEntity>()
                  .WithMany()
                  .HasForeignKey(report => report.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(report => report.Number)
                  .UseIdentityAlwaysColumn();
        });

        builder.Entity<ExpenseEntity>(entity =>
        {
            entity.ToTable("expenses", table =>
            {
                table.HasCheckConstraint("CK_expenses_Amount", "\"Amount\" > 0 AND \"Amount\" = round(\"Amount\", 2)");
            });

            entity.HasOne<ExpenseReportEntity>()
                  .WithMany()
                  .HasForeignKey(expense => expense.ExpenseReportId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    internal static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<UserEntity>().AnyAsync(cancellationToken))
            return;

        context.Set<UserEntity>().AddRange(
            new UserEntity { FirstName = "Juste", LastName = "Leblanc", Street = "10 rue des Lilas", PostalCode = "75001", City = "Paris", MaxExpensesPerMonth = 5, IsActive = true },
            new UserEntity { FirstName = "Marc", LastName = "Assin", Street = "20 rue des Roses", PostalCode = "69001", City = "Lyon", MaxExpensesPerMonth = 10, IsActive = true }
        );

        await context.SaveChangesAsync(cancellationToken);
    }
}