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
  public DbSet<Template> Templates { get; set; } = null!;
  public DbSet<TemplateColumn> TemplateColumns { get; set; } = null!;
  public DbSet<TemplateItem> TemplateItems { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.UseCollation("Czech_CI_AS");

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
      entity.HasIndex(e => e.Email).IsUnique();
    });

    modelBuilder.Entity<Project>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
      entity.Property(e => e.Description).HasMaxLength(2000);
      entity
        .HasMany(e => e.Roles)
        .WithOne(r => r.Project)
        .HasForeignKey(r => r.ProjectId)
        .OnDelete(DeleteBehavior.Restrict);
      entity.HasIndex(e => e.Title).IsUnique();
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

    modelBuilder.Entity<Template>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Type).IsRequired();
      entity
        .HasOne(e => e.Project)
        .WithMany()
        .HasForeignKey(e => e.ProjectId)
        .OnDelete(DeleteBehavior.Restrict);
      entity
        .HasMany(e => e.TemplateColumns)
        .WithOne(c => c.Template)
        .HasForeignKey(c => c.TemplateId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<TemplateColumn>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.WidthWeight).IsRequired();
      entity
        .HasOne(e => e.Template)
        .WithMany(t => t.TemplateColumns)
        .HasForeignKey(e => e.TemplateId)
        .OnDelete(DeleteBehavior.Restrict);
      entity
        .HasMany(e => e.TemplateItems)
        .WithOne(i => i.TemplateColumn)
        .HasForeignKey(i => i.TemplateColumnId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<TemplateItem>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedOnAdd();
      entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
      entity.Property(e => e.OrderIndex).IsRequired();
      entity.Property(e => e.ColumnIndex).IsRequired();
      entity.Property(e => e.Type).IsRequired();
      entity.Property(e => e.ValidatingRegex).HasMaxLength(1024);
      entity
        .HasOne(e => e.TemplateColumn)
        .WithMany(c => c.TemplateItems)
        .HasForeignKey(e => e.TemplateColumnId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    base.OnModelCreating(modelBuilder);
  }
}
