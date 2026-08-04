using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Shared.Base;
using System.Text.Json;

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
    public DbSet<BookBorrowRecord> BookBorrowRecords
    => Set<BookBorrowRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

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
        return SaveChangesWithAudit();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pending = CapturePendingAudits();
        StampAuditFields();
        var ownsTransaction = Database.IsRelational() && Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var affected = await base.SaveChangesAsync(cancellationToken);
            if (pending.Count > 0)
            {
                AuditLogs.AddRange(pending.Select(CreateAuditLog));
                await base.SaveChangesAsync(cancellationToken);
            }
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private int SaveChangesWithAudit()
    {
        var pending = CapturePendingAudits();
        StampAuditFields();
        using var transaction = Database.IsRelational() && Database.CurrentTransaction is null
            ? Database.BeginTransaction()
            : null;
        try
        {
            var affected = base.SaveChanges();
            if (pending.Count > 0)
            {
                AuditLogs.AddRange(pending.Select(CreateAuditLog));
                base.SaveChanges();
            }
            transaction?.Commit();
            return affected;
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
    }

    private List<PendingAudit> CapturePendingAudits()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<AuditLog>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Audit logs are immutable.");
        }

        return ChangeTracker.Entries<AuditableEntity>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry =>
            {
                var action = entry.State switch
                {
                    EntityState.Added => AuditAction.Created,
                    EntityState.Deleted => AuditAction.Deleted,
                    _ => AuditAction.Updated
                };
                return new PendingAudit(
                    entry,
                    action,
                    action == AuditAction.Updated ? SerializeChanges(entry) : "{}",
                    DateTime.UtcNow);
            })
            .ToList();
    }

    private AuditLog CreateAuditLog(PendingAudit pending)
    {
        var entity = pending.Entry.Entity;
        var actorName = _baseService?.UserDisplayName;
        var actorEmail = _baseService?.UserEmail;
        return new AuditLog
        {
            EntityType = GetEntityType(entity),
            EntityId = entity.Id,
            EntityDisplayName = GetDisplayName(entity),
            Action = pending.Action,
            ActorUserId = _baseService?.UserId,
            ActorDisplayName = !string.IsNullOrWhiteSpace(actorName)
                ? actorName
                : !string.IsNullOrWhiteSpace(actorEmail) ? actorEmail : "System",
            ActorEmail = actorEmail,
            OccurredAtUtc = pending.OccurredAtUtc,
            ChangeDetailsJson = pending.ChangeDetailsJson,
            IsBackfilled = false
        };
    }

    private static string SerializeChanges(EntityEntry<AuditableEntity> entry)
    {
        var changes = new Dictionary<string, object?>();
        foreach (var property in entry.Properties.Where(x => x.IsModified))
        {
            var name = property.Metadata.Name;
            if (AuditHousekeepingProperties.Contains(name))
                continue;

            if (IsSecret(name))
            {
                changes["Password"] = new { changed = true };
                continue;
            }

            changes[name] = new
            {
                oldValue = SafeValue(property.OriginalValue),
                newValue = SafeValue(property.CurrentValue)
            };
        }
        return JsonSerializer.Serialize(changes);
    }

    private static object? SafeValue(object? value) =>
        value is DateTime dateTime ? dateTime.ToUniversalTime() : value;

    private static bool IsSecret(string name) =>
        name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Secret", StringComparison.OrdinalIgnoreCase);

    private static readonly HashSet<string> AuditHousekeepingProperties =
    [
        nameof(BaseEntity.CreatedAt),
        nameof(BaseEntity.UpdatedAt),
        nameof(AuditableEntity.CreatedBy),
        nameof(AuditableEntity.UpdatedBy),
        nameof(AuditableEntity.DeletedBy),
        nameof(AuditableEntity.DeletedAt),
        nameof(AuditableEntity.IsDeleted)
    ];

    private static string GetEntityType(AuditableEntity entity) => entity switch
    {
        BookBorrowRecord => "Borrow Record",
        User => "Account",
        _ => entity.GetType().Name
    };

    private static string GetDisplayName(AuditableEntity entity) => entity switch
    {
        User user => FirstReadable(user.FullName, user.Email, $"Account #{user.Id}"),
        Role role => FirstReadable(role.Name, $"Role #{role.Id}"),
        Author author => FirstReadable(author.Name, $"Author #{author.Id}"),
        Category category => FirstReadable(category.Name, $"Category #{category.Id}"),
        Book book => FirstReadable(book.Title, $"Book #{book.Id}"),
        BookBorrowRecord record => $"Borrow record #{record.Id}",
        _ => $"{entity.GetType().Name} #{entity.Id}"
    };

    private static string FirstReadable(params string[] values) =>
        values.First(x => !string.IsNullOrWhiteSpace(x));

    private sealed record PendingAudit(
        EntityEntry<AuditableEntity> Entry,
        AuditAction Action,
        string ChangeDetailsJson,
        DateTime OccurredAtUtc);

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
                    entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified = false;
                    entry.Property(nameof(AuditableEntity.UpdatedBy)).IsModified = false;
                    deleted.IsDeleted = true;
                    deleted.DeletedBy = userId;
                    deleted.DeletedAt = now;
                    break;
            }
        }
    }
}
