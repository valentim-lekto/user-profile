using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserProfile.Api.Data;

// Discovered by EF Core tools through IDesignTimeDbContextFactory<TContext>.
// ReSharper disable once UnusedType.Global
public sealed class UserProfileDbContextFactory : IDesignTimeDbContextFactory<UserProfileDbContext>
{
    public UserProfileDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UserProfileDbContext>()
            .UseSqlite("Data Source=user-profile.design.db")
            .Options;

        return new UserProfileDbContext(options);
    }
}
