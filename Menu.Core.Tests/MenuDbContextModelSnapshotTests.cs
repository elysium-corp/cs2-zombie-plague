using System.Reflection;
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
        var snapshotModel = context.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model);
        var currentModel = context.GetService<IDesignTimeModel>().Model;
        var modelDiffer = context.GetService<IMigrationsModelDiffer>();

        var operations = modelDiffer.GetDifferences(
            snapshotModel.GetRelationalModel(),
            currentModel.GetRelationalModel());

        Assert.True(
            operations.Count == 0,
            "EF model snapshot drift:" + Environment.NewLine +
            string.Join(Environment.NewLine, operations.Select(DescribeOperation)));
    }

    private static string DescribeOperation(object operation) =>
        DescribeObject(operation, depth: 0);

    private static string DescribeObject(object value, int depth)
    {
        var properties = value.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => $"{property.Name}={ReadProperty(value, property, depth)}");

        return $"{value.GetType().Name}({string.Join(", ", properties)})";
    }

    private static string ReadProperty(object owner, PropertyInfo property, int depth)
    {
        try
        {
            return FormatValue(property.GetValue(owner), depth);
        }
        catch (Exception exception)
        {
            return $"<throws {exception.GetType().Name}>";
        }
    }

    private static string FormatValue(object? value, int depth)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string text)
        {
            return $"\"{text}\"";
        }

        if (value is Type type)
        {
            return type.FullName ?? type.Name;
        }

        if (value is System.Collections.IEnumerable values)
        {
            return "[" + string.Join(", ", values.Cast<object?>().Select(item => FormatValue(item, depth + 1))) + "]";
        }

        var valueType = value.GetType();
        if (depth == 0 && valueType.Namespace?.StartsWith(
                "Microsoft.EntityFrameworkCore.Migrations.Operations",
                StringComparison.Ordinal) == true)
        {
            return DescribeObject(value, depth + 1);
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? valueType.Name;
    }
}
