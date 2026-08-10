using EMS.Core.Entities;
using EMS.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // ============================================================
        // 1. Employee Configuration
        // ============================================================
        modelBuilder.Entity<Employee>(entity =>
        {
            // Primary Key
            entity.HasKey(e => e.Id);

            // Properties Columns 
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);

            // 🔥 Unique Constraint - Duplicate Email नहीं हो सकती
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Salary).HasPrecision(18, 2);
            entity.Property(e => e.PhoneNumber).HasMaxLength(10);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.PresentAddress).HasMaxLength(500);
            entity.Property(e => e.PreviousCompanyRole).HasMaxLength(200);

            // 🔥 Relationship: Employee → User (One-to-One)
            // एक Employee का सिर्फ 1 User हो सकता है
            entity.HasOne(e => e.User).WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)  // FK: Employee.UserId → User.Id
            .OnDelete(DeleteBehavior.SetNull);  // अगर User Delete हो तो Employee.UserId NULL हो जाए

            // 🔥 Index for faster search
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.IsActive);
        });


        // ============================================================
        // 1. User Configuration
        // ============================================================
        modelBuilder.Entity<User>(entity =>
        {
            // Primary Key
            entity.HasKey(u => u.Id);

            // Properties Columns 
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);

            // 🔥 Unique Constraint - Duplicate Email नहीं हो सकती
            entity.HasIndex(u => u.Email).IsUnique();

            // 🔥 Role Enum को int (0,1,2) में Store करो
            entity.Property(u => u.Role).HasConversion<int>();

            // 🔥 Relationship: User → Employee (One-to-One) - Reverse Side
            entity.HasOne(u => u.Employee).WithOne(e => e.User)
            .HasForeignKey<User>(u => u.EmployeeId)  // FK: EmployeeId → Employee.Id
            .OnDelete(DeleteBehavior.SetNull);

            // 🔥 Relationship: User → RefreshTokens (One-to-Many)
            // एक User के Multiple Refresh Tokens हो सकते हैं
            entity.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);  // अगर User Delete हो तो उसके सारे Tokens Delete हो जाएं

            // 🔥 Index for faster search
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Role);
        });

        // ============================================================
        // 3. RefreshToken Configuration (आपका पहले से है, लेकिन इसे पूरा करते हैं)
        // ============================================================
        // 👇 RefreshToken के लिए Index (Performance के लिए)
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            // 🔥 Primary Key
            entity.HasKey(rt => rt.Id);

            // 🔥 Token Column
            entity.Property(rt => rt.Token).IsRequired().HasMaxLength(500);

            // 🔥 Unique Constraint - Duplicate Token नहीं हो सकता
            entity.HasIndex(rt => rt.Token).IsUnique();

            // 🔥 UserId Index (Performance के लिए)
            entity.HasIndex(rt => rt.UserId);

            // 🔥 Relationship: RefreshToken → User (Many-to-One)
            entity.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            // 🔥 Expiry और Revoke के लिए Index
            entity.HasIndex(rt => rt.ExpiresAt);
            entity.HasIndex(rt => rt.IsRevoked);
        });
    }
}