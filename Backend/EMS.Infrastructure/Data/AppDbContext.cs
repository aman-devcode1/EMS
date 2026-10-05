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
    public DbSet<OtpCode> OtpCodes { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Designation> Designations { get; set; }


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

            // Unique Constraint - No Duplicate Email
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Salary).HasPrecision(18, 2);
            entity.Property(e => e.PhoneNumber).HasMaxLength(10);
            // entity.Property(e => e.Department).HasMaxLength(100);
            // entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.PresentAddress).HasMaxLength(500);
            entity.Property(e => e.PreviousCompanyRole).HasMaxLength(200);

            // Relationship: Employee → User (One-to-One)
            // One Employe have only one User
            entity.HasOne(e => e.User).WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)  // FK: Employee.UserId → User.Id
            .OnDelete(DeleteBehavior.SetNull);  // User Delete - Employee.UserId NULL

            // Employee → Department (Many-to-One)
            entity.HasOne(e => e.Designation).WithMany().HasForeignKey(d => d.DesignationId).OnDelete(DeleteBehavior.SetNull);

            // Employee → Designation (Many-to-One)
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(d => d.DepartmentId).OnDelete(DeleteBehavior.SetNull);

            // Index for faster search
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.DesignationId);
            entity.HasIndex(e => e.ScheduledForDeletionAt);
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

            // Unique Constraint - Duplicate Email नहीं हो सकती
            entity.HasIndex(u => u.Email).IsUnique();

            // Role Enum को int (0,1,2) में Store करो
            entity.Property(u => u.Role).HasConversion<int>();

            // Relationship: User → RefreshTokens (One-to-Many)
            // एक User के Multiple Refresh Tokens हो सकते हैं
            entity.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);  // अगर User Delete हो तो उसके सारे Tokens Delete हो जाएं

            // Index for faster search
            entity.HasIndex(e => e.Role);
        });

        // ============================================================
        // 3. RefreshToken Configuration (आपका पहले से है, लेकिन इसे पूरा करते हैं)
        // ============================================================
        // RefreshToken के लिए Index (Performance के लिए)
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            // Primary Key
            entity.HasKey(rt => rt.Id);

            // Token Column
            entity.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(64);

            // Unique Constraint - Duplicate Token नहीं हो सकता
            entity.HasIndex(rt => rt.TokenHash).IsUnique();

            // UserId Index (Performance के लिए)
            entity.HasIndex(rt => rt.UserId);

            // Relationship: RefreshToken → User (Many-to-One)
            entity.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            // Expiry और Revoke के लिए Index
            entity.HasIndex(rt => rt.AbsoluteExpiresAt);
            entity.HasIndex(rt => rt.IsRevoked);
        });

        // ============================================================
        // 4. OtpCode Configuration
        // ============================================================
        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Code).IsRequired().HasMaxLength(10);
            entity.Property(o => o.Purpose).HasConversion<int>();

            // Ek user ka sirf 1 hi active OTP row ho sakta hai
            entity.HasIndex(o => o.UserId).IsUnique();

            entity.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(o => o.ExpiresAt);
        });

        // ============================================================
        // 5. Department Configuration
        // ============================================================
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Description).HasMaxLength(500);

            // Unique name (Case-sensitive SQL Server default)
            entity.HasIndex(d => d.Name).IsUnique();
            entity.HasIndex(d => d.IsActive);
        });

        // ============================================================
        // 5. Designation Configuration
        // ============================================================
        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Description).HasMaxLength(500);

            // Unique name (Case-sensitive SQL Server default)
            entity.HasIndex(d => d.Name).IsUnique();
            entity.HasIndex(d => d.IsActive);
        });
    }

    // ============================================================
    // AUTO-SET AUDIT FIELDS ON SAVE
    // - CreatedAt: pehli baar insert hone par
    // - UpdatedAt: har save par (insert + update)
    // ============================================================
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Set UpdateAt in every insert / update
            entry.Entity.UpdatedAt = DateTime.UtcNow;

            // Don't override the CreatedAt while insert (if already set)
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    // Override the sync version (if called)
    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Set UpdateAt in every insert / update
            entry.Entity.UpdatedAt = DateTime.UtcNow;

            // Don't override the CreatedAt while insert (if already set)
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChanges();
    }
}