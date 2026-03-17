using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Model.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<AppUser> AppUsers { get; set; } = null!;
  public DbSet<Project> Projects { get; set; } = null!;
  public DbSet<Role> Roles { get; set; } = null!;
  public DbSet<Membership> Memberships { get; set; } = null!;
  public DbSet<Item> Items { get; set; } = null!;
  public DbSet<Sprint> Sprints { get; set; } = null!;
  public DbSet<SprintItem> SprintItems { get; set; } = null!;
  public DbSet<WorkflowState> WorkflowStates { get; set; } = null!;
  public DbSet<Token> Tokens { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<AppUser>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
      entity.Property(e => e.Name).HasMaxLength(128);
      entity.Property(e => e.Surname).HasMaxLength(128);
      entity
        .HasMany(e => e.Tokens)
        .WithOne(t => t.AppUser)
        .HasForeignKey(t => t.AppUserId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Project>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
      entity.Property(e => e.Description).HasMaxLength(2000);
      entity
        .HasOne(e => e.Owner)
        .WithMany(u => u.OwnedProjects)
        .HasForeignKey(e => e.OwnerId)
        .OnDelete(DeleteBehavior.Restrict);
      entity
        .HasMany(e => e.Roles)
        .WithOne(r => r.Project)
        .HasForeignKey(r => r.ProjectId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Role>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Title).IsRequired().HasMaxLength(128);
      entity
        .HasOne(e => e.Project)
        .WithMany(p => p.Roles)
        .HasForeignKey(e => e.ProjectId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Membership>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Role).IsRequired().HasMaxLength(128);
      entity
        .HasOne(e => e.Project)
        .WithMany(p => p.Memberships)
        .HasForeignKey(e => e.ProjectId)
        .OnDelete(DeleteBehavior.Restrict);
      entity
        .HasOne(e => e.User)
        .WithMany(u => u.Memberships)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Item>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Title).IsRequired().HasMaxLength(512);
      entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(e => e.Assignee).WithMany().HasForeignKey(e => e.AssigneeId).OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Sprint>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
      entity.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<SprintItem>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(e => e.Sprint).WithMany().HasForeignKey(e => e.SprintId).OnDelete(DeleteBehavior.Restrict);
      entity
        .HasOne(e => e.WorkflowState)
        .WithMany()
        .HasForeignKey(e => e.WorkflowStateId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Token>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Value).IsRequired().HasMaxLength(512);
      entity
        .HasOne(e => e.AppUser)
        .WithMany(u => u.Tokens)
        .HasForeignKey(e => e.AppUserId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<WorkflowState>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.OrderIndex).IsRequired();
      entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
      entity.Property(e => e.Description).HasMaxLength(2000);
      entity.Property(e => e.Type).IsRequired();
      entity
        .HasOne(e => e.Project)
        .WithMany(p => p.WorkflowStates)
        .HasForeignKey(e => e.ProjectId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    base.OnModelCreating(modelBuilder);
  }
}
