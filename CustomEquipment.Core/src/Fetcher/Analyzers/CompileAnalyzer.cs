using System.Reflection;
using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Fetcher.Analyzers;

internal class CompileAnalyzer<TItem>(Assembly assembly) : IAnalyzer<TItem> where TItem : IItem
{
    public HashSet<TItem> Analyze()
    {
        var itemType = typeof(TItem);

        return assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                itemType.IsAssignableFrom(type)
            )
            .Select(CreateItem)
            .ToHashSet();
    }

    private static TItem CreateItem(Type type)
    {
        return Activator.CreateInstance(type, nonPublic: true) is TItem item
            ? item
            : throw new InvalidOperationException($"Could not create equipment item '{type.FullName}'!");
    }
}