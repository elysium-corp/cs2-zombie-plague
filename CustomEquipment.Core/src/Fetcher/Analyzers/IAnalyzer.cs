using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Fetcher.Analyzers;

internal interface IAnalyzer<TItem> where TItem : IItem
{
    HashSet<TItem> Analyze();
}