using Microsoft.EntityFrameworkCore;

namespace JadaraClearance.Models;

public class JadaraClearanceDbContext : DbContext
{
    public JadaraClearanceDbContext(DbContextOptions<JadaraClearanceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; } = null!;
    public virtual DbSet<Department> Departments { get; set; } = null!;
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<ClearanceRequest> ClearanceRequests { get; set; } = null!;
    public virtual DbSet<ClearanceApproval> ClearanceApprovals { get; set; } = null!;
    public virtual DbSet<ClearanceAttachment> ClearanceAttachments { get; set; } = null!;
    public virtual DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
        });

        // Department configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(100);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.UniversityId).HasMaxLength(50);

            entity.HasOne(d => d.Role)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Department)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ClearanceRequest configuration
        modelBuilder.Entity<ClearanceRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OverallStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CertificateHash).HasMaxLength(256);

            entity.HasOne(d => d.Student)
                .WithMany(p => p.ClearanceRequests)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ClearanceApproval configuration
        modelBuilder.Entity<ClearanceApproval>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FineAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(d => d.Request)
                .WithMany(p => p.ClearanceApprovals)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Department)
                .WithMany(p => p.ClearanceApprovals)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ActionByOfficer)
                .WithMany(p => p.ClearanceApprovals)
                .HasForeignKey(d => d.ActionByOfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ClearanceAttachment configuration
        modelBuilder.Entity<ClearanceAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);

            entity.HasOne(d => d.Request)
                .WithMany(p => p.ClearanceAttachments)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Approval)
                .WithMany(p => p.ClearanceAttachments)
                .HasForeignKey(d => d.ApprovalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.UploadedByUser)
                .WithMany(p => p.ClearanceAttachments)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);

            entity.HasOne(d => d.Request)
                .WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.ActionByUser)
                .WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActionByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
