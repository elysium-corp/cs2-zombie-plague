using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
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

    /// <summary>Создаёт экземпляр зарегистрированного предмета.</summary>
    IItem CreateItem(string internalName);

    /// <summary>Ключ общей регистрации API.</summary>
    static readonly string SharedApiKey = "CustomEquipment.Api.ICustomEquipmentApi";
}
