using Admin.Core.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Admin.Core.Tests;

public sealed class AdminMigrationTests
{
    [Fact]
    public void UpdatedSchema_HasNoUnmigratedModelChanges()
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=metadata;Password=metadata")
            .Options;
        using var context = new AdminDbContext(options);
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
