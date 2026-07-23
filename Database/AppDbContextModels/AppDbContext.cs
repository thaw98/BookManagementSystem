using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Shared.Base;

namespace Database.AppDbContextModels;

public class AppDbContext : DbContext
{
    private readonly IBaseService? _baseService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IBaseService? baseService = null)
        : base(options)
    {
        _baseService = baseService;
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(
                Expression.Property(param, nameof(AuditableEntity.IsDeleted)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, param));
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditFields()
    {
        var now = DateTime.UtcNow;
        var userId = _baseService?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    if (entry.Entity is AuditableEntity created)
                        created.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    if (entry.Entity is AuditableEntity updated)
                        updated.UpdatedBy = userId;
                    break;

                case EntityState.Deleted when entry.Entity is AuditableEntity deleted:
                    entry.State = EntityState.Modified;
                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    deleted.IsDeleted = true;
                    deleted.UpdatedAt = now;
                    deleted.UpdatedBy = userId;
                    break;
            }
        }
    }
}
