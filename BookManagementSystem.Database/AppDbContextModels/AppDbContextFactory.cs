using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookManagementSystem.Database.AppDbContextModels;

public class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "server=localhost;" +
            "port=3306;" +
            "database=book_management_system;" +
            "user=root;" +
            "password=root;";

        var optionsBuilder =
            new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 32)));

        return new AppDbContext(optionsBuilder.Options);
    }
}