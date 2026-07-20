namespace BookManagementSystem.Domain.Seeding;

public interface IDbSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
