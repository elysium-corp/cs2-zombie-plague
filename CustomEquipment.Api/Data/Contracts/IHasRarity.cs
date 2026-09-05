using CustomEquipment.Api.Enums;

namespace CustomEquipment.Api.Data.Contracts;

/// <summary>Предоставляет редкость предмета независимо от цены и способа его покупки.</summary>
public interface IHasRarity
{
    /// <summary>Редкость из каталога экипировки, определяющая цвет названия предмета.</summary>
    ItemRarity Rarity { get; }
}
