namespace CustomEquipment.Api.Data.Contracts;

/// <summary>Собственная иконка предмета для магазина и пользовательских HUD.</summary>
public interface IHasHudIcon
{
    /// <summary>Путь panorama/images/...vsvg внутри VPK; null означает иконку базового оружия.</summary>
    string? HudIconPath { get; }
}
