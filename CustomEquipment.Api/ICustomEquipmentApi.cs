using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Registration;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api;

/// <summary>
/// Общедоступный API пользовательского снаряжения.
/// </summary>
public interface ICustomEquipmentApi
{
    /// <summary>Регистратор пользовательских предметов.</summary>
    IEquipmentRegistrar Registrar { get; }

    /// <summary>События пользовательского снаряжения.</summary>
    ICustomEquipmentEvents Events { get; }

    /// <summary>Возвращает все зарегистрированные предметы.</summary>
    IReadOnlyCollection<IItem> GetRegisteredItems();

    /// <summary>Пытается найти зарегистрированный предмет.</summary>
    bool TryGetRegisteredItem(string internalName, [NotNullWhen(true)] out IItem? item);

    /// <summary>Выдаёт зарегистрированный предмет игроку.</summary>
    void GiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop);

    /// <summary>Пытается выдать зарегистрированный предмет и сообщает результат.</summary>
    /// <remarks>Реализация по умолчанию сохраняет совместимость со старыми поставщиками API.</remarks>
    bool TryGiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        GiveItem(player, internalName, action);
        return true;
    }

    /// <summary>Проверяет доступность зарегистрированного предмета для текущей стороны игрока.</summary>
    bool CanUseItem(IPlayer player, string internalName) => false;

    /// <summary>Пытается получить активное пользовательское оружие игрока.</summary>
    bool TryGetActiveWeapon(IPlayer player, [NotNullWhen(true)] out IWeapon? weapon)
    {
        weapon = null;
        return false;
    }

    /// <summary>
    /// Проверяет, можно ли увеличить резерв активного пользовательского оружия,
    /// если его internal name совпадает с ожидаемым.
    /// </summary>
    bool CanRefillActiveWeapon(IPlayer player, string expectedInternalName) => false;

    /// <summary>
    /// Пополняет резерв активного пользовательского оружия, если его internal name совпадает
    /// с ожидаемым. Метод не выполняет денежных операций.
    /// </summary>
    bool TryRefillActiveWeapon(
        IPlayer player,
        string expectedInternalName,
        int amount,
        out AmmoRefillResult result)
    {
        result = default;
        return false;
    }

    /// <summary>Создаёт экземпляр зарегистрированного предмета.</summary>
    IItem CreateItem(string internalName);

    /// <summary>Ключ общей регистрации API.</summary>
    static readonly string SharedApiKey = "CustomEquipment.Api.ICustomEquipmentApi";
}
