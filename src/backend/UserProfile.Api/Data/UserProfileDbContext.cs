using Microsoft.EntityFrameworkCore;

namespace UserProfile.Api.Data;

public sealed class UserProfileDbContext(DbContextOptions<UserProfileDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserProfileDbContext).Assembly);
    }
}
