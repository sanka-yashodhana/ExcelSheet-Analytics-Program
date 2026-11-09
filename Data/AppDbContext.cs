using Microsoft.EntityFrameworkCore;
using EmployeeExcelAnalytics.Api.Models;

namespace EmployeeExcelAnalytics.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<ImportHistory> ImportHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ImportHistory>()
                .ToTable("ImportHistory");

            modelBuilder.Entity<Employee>()
                .ToTable("Employees");
        }
    }

}