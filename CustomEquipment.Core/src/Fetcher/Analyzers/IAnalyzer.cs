using CustomEquipment.Data.Equipments.Contracts;

namespace CustomEquipment.Fetcher.Analyzers;

internal interface IAnalyzer<TItem> where TItem : IItem
{
    HashSet<TItem> Analyze();
}