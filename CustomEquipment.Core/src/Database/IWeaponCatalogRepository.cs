using CustomEquipment.Data.DatabaseWeapons;

namespace CustomEquipment.Database;

internal interface IWeaponCatalogRepository
{
    IReadOnlyCollection<DatabaseWeaponItem> GetEnabledWeapons();
}
