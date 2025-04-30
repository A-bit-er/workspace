using Microsoft.EntityFrameworkCore;
using ADScimApi.Models;
using BCrypt.Net;

namespace ADScimApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BusinessRule> BusinessRules { get; set; }
    public DbSet<OrganizationalUnit> OrganizationalUnits { get; set; }
    public DbSet<ApiUser> ApiUsers { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entity relationships and constraints
        modelBuilder.Entity<BusinessRule>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<OrganizationalUnit>()
            .HasKey(ou => ou.Id);

        modelBuilder.Entity<ApiUser>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<AuditLog>()
            .HasKey(a => a.Id);

        // Seed initial data
        modelBuilder.Entity<ApiUser>().HasData(
            new ApiUser
            {
                Id = 1,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                IsActive = true
            }
        );

        modelBuilder.Entity<OrganizationalUnit>().HasData(
            new OrganizationalUnit
            {
                Id = 1,
                Name = "Users",
                DistinguishedName = "OU=Users,DC=example,DC=com",
                Description = "Default Users OU"
            },
            new OrganizationalUnit
            {
                Id = 2,
                Name = "Groups",
                DistinguishedName = "OU=Groups,DC=example,DC=com",
                Description = "Default Groups OU"
            },
            new OrganizationalUnit
            {
                Id = 3,
                Name = "Service Accounts",
                DistinguishedName = "OU=Service Accounts,DC=example,DC=com",
                Description = "Default Service Accounts OU"
            }
        );
    }
}