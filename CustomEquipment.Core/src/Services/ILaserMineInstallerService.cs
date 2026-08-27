using CustomEquipment.Data.Equipments.Weapons.Equipments;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

public interface ILaserMineInstallerService
{
    public bool TrySetup(IPlayer player, LaserMine mine);
}