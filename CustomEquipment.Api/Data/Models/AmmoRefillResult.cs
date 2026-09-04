namespace CustomEquipment.Api.Data.Models;

/// <summary>Результат пополнения резерва активного пользовательского оружия.</summary>
public readonly record struct AmmoRefillResult(int AddedAmount, int ReserveAmmo);
