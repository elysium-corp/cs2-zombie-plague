namespace CustomEquipment.Api.Data.Contracts;

/// <summary>
/// Предоставляет явный ключ Localization.Core для отображаемого названия товара.
/// </summary>
public interface ILocalizedShopItem
{
    /// <summary>
    /// Возвращает ключ локализации названия товара.
    /// </summary>
    string DisplayNameKey { get; }
}
