using CustomEquipment.Data.Equipments.Contracts;

namespace CustomEquipment.Fetcher;

internal interface IEquipmentFetcher
{
    public HashSet<IItem> Fetch();
}