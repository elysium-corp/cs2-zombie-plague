using CustomEquipment.Data.Equipments.Weapons.Equipments;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

/// <summary>
/// Управляет отложенной установкой лазерных мин.
/// </summary>
public interface ILaserMineInstallerService
{
    /// <summary>
    /// Начинает установку мины, если игрок и выбранная поверхность подходят.
    /// </summary>
    /// <param name="player">Игрок, устанавливающий мину.</param>
    /// <param name="mine">Предмет мины из инвентаря игрока.</param>
    /// <returns><see langword="true" />, если установка запущена.</returns>
    bool TrySetup(IPlayer player, LaserMine mine);

    /// <summary>
    /// Отменяет текущую установку мины указанного игрока.
    /// </summary>
    /// <param name="player">Игрок, чью установку нужно отменить.</param>
    void Cancel(IPlayer player);
}
