using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Fetcher.Analyzers;

namespace CustomEquipment.Fetcher;

internal sealed class EquipmentFetcher(IAnalyzer<IItem> compileAnalyzer) : IEquipmentFetcher
{
    public HashSet<IItem> Fetch()
    {
        var compileWeapons = compileAnalyzer.Analyze();
        return compileWeapons;
    }
}