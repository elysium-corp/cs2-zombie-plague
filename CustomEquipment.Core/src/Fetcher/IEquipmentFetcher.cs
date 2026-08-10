using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Fetcher;

internal interface IEquipmentFetcher
{
    public HashSet<IItem> Fetch();
}