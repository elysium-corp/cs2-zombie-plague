using System.Reflection;
using CustomEquipment.Api.Data.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CustomEquipment.Fetcher.Analyzers;

internal class CompileAnalyzer<TItem>(Assembly assembly, IServiceProvider serviceProvider) : IAnalyzer<TItem>
    where TItem : IItem
{
    public HashSet<TItem> Analyze()
    {
        var itemType = typeof(TItem);

        return assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Namespace?.StartsWith("CustomEquipment.Data.Equipments.", StringComparison.Ordinal) == true &&
                itemType.IsAssignableFrom(type)
            )
            .Select(CreateItem)
            .ToHashSet();
    }

    private TItem CreateItem(Type type)
    {
        return ActivatorUtilities.CreateInstance(serviceProvider, type) is TItem item
            ? item
            : throw new InvalidOperationException($"Could not create equipment item '{type.FullName}'!");
    }
}
