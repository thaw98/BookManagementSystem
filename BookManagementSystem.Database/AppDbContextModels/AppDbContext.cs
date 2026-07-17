using Microsoft.EntityFrameworkCore;

namespace Database.AppDbContextModels;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}