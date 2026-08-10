using System.Reflection;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Fetcher.Analyzers;

internal class CompileAnalyzer<TItem> : IAnalyzer<TItem> where TItem : IItem
{
    public HashSet<TItem> Analyze()
    {
        var baseType = typeof(TItem);

        return Assembly.GetAssembly(baseType)!
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
            .Select(type => (TItem)Activator.CreateInstance(type)!)
            .ToHashSet();
    }
}