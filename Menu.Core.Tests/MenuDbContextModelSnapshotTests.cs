using Menu.Core.Database;
using Menu.Core.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Menu.Core.Tests;

public sealed class MenuDbContextModelSnapshotTests
{
    [Fact]
    public void Snapshot_MatchesCurrentEfModel()
    {
        using var context = new MenuDbContextDesignTimeFactory().CreateDbContext([]);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var snapshot = Assert.IsType<MenuDbContextModelSnapshot>(migrationsAssembly.ModelSnapshot);
        var currentModel = context.GetService<IDesignTimeModel>().Model;
        var modelDiffer = context.GetService<IMigrationsModelDiffer>();

        var operations = modelDiffer.GetDifferences(
            snapshot.Model.GetRelationalModel(),
            currentModel.GetRelationalModel());

        Assert.Empty(operations);
    }
}
