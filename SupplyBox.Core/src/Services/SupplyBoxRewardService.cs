using CustomEquipment.Api;
using CustomEquipment.Api.Events.Contexts.Items;
using Economy.Api;
using Microsoft.Extensions.Logging;
using SupplyBox.Configuration;
using SupplyBox.Data.Configs;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace SupplyBox.Services;

internal sealed class SupplyBoxRewardService(ISwiftlyCore core, IEconomyApi economy, ICustomEquipmentApi equipment)
{
    private readonly Dictionary<ulong, (int Count, long Last)> _collections = [];
    public void ResetRound() => _collections.Clear();
    public bool CanCollect(IPlayer player, SupplyBoxConfig settings)
    {
        var key = player.IsAuthorized ? player.SteamID : unchecked((ulong)(uint)player.PlayerID);
        if (!_collections.TryGetValue(key, out var state)) return true;
        return (settings.MaxCollectionsPerPlayerPerRound == 0 || state.Count < settings.MaxCollectionsPerPlayerPerRound)
            && Environment.TickCount64 - state.Last >= settings.PlayerCooldownSeconds * 1000L;
    }

    public bool TryGrant(IPlayer player, SupplyBoxType box, SupplyBoxConfig settings)
    {
        if (!CanCollect(player, settings)) return false;
        var pool = box.Loot.Where(loot => loot.Enabled && IsAvailable(player, loot, settings)).ToList();
        var granted = false;
        for (var roll = 0; roll < box.Rolls && pool.Count > 0; roll++)
        {
            var reward = Weighted(pool, item => item.Weight);
            try { granted |= Grant(player, reward, settings); }
            catch (Exception exception) { core.Logger.LogError(exception, "[SupplyBox] Reward {Kind}/{Item} failed.", reward.Kind, reward.ItemKey); }
            if (box.UniqueRewards) pool.Remove(reward);
            pool.RemoveAll(item => !IsAvailable(player, item, settings));
        }
        if (granted)
        {
            var key = player.IsAuthorized ? player.SteamID : unchecked((ulong)(uint)player.PlayerID);
            _collections.TryGetValue(key, out var state);
            _collections[key] = (state.Count + 1, Environment.TickCount64);
        }
        return granted;
    }

    private bool IsAvailable(IPlayer player, SupplyBoxLoot loot, SupplyBoxConfig settings) => loot.Kind switch
    {
        "money" => economy.HasEnoughMoney(player, 0),
        "health" => player.RequiredPlayerPawn.Health < settings.HealthCap,
        "armor" => player.RequiredPlayerPawn.ArmorValue < settings.ArmorCap,
        "equipment" => equipment.CanUseItem(player, loot.ItemKey),
        "weapon" => !SupplyBox.ZombiePlagueApi.IsInfected(player)
            && player.RequiredPlayerPawn is { ItemServices: not null, WeaponServices: not null },
        _ => false
    };

    private bool Grant(IPlayer player, SupplyBoxLoot loot, SupplyBoxConfig settings)
    {
        var pawn = player.RequiredPlayerPawn;
        var amount = Random.Shared.Next(loot.MinAmount, loot.MaxAmount + 1);
        switch (loot.Kind)
        {
            case "money":
                var before = economy.GetBalance(player);
                economy.GiveMoney(player, amount);
                return economy.GetBalance(player) > before;
            case "health":
                var health = (int)Math.Min(settings.HealthCap, (long)pawn.Health + amount);
                if (health <= pawn.Health) return false;
                pawn.Health = health; pawn.HealthUpdated(); return true;
            case "armor":
                var armor = (int)Math.Min(settings.ArmorCap, (long)pawn.ArmorValue + amount);
                if (armor <= pawn.ArmorValue) return false;
                pawn.ArmorValue = armor; pawn.ArmorValueUpdated(); return true;
            case "equipment": return GrantEquipment(player, loot.ItemKey);
            case "weapon":
                if (!SupplyBoxDocument.StandardWeapons.Contains(loot.ItemKey)) return false;
                var pistols = new[] { "weapon_glock", "weapon_hkp2000", "weapon_usp_silencer", "weapon_elite", "weapon_p250", "weapon_tec9", "weapon_fiveseven", "weapon_cz75a", "weapon_deagle", "weapon_revolver" };
                pawn.WeaponServices!.DropWeaponBySlot(pistols.Contains(loot.ItemKey) ? gear_slot_t.GEAR_SLOT_PISTOL : gear_slot_t.GEAR_SLOT_RIFLE);
                return pawn.ItemServices!.GiveItem<CCSWeaponBase>(loot.ItemKey) is { IsValid: true };
            default: return false;
        }
    }

    private bool GrantEquipment(IPlayer player, string key)
    {
        var granted = false;
        void OnGiven(ref ItemGivenContext context)
        {
            if (context.Player.PlayerID == player.PlayerID && context.Item.InternalName == key) granted = true;
        }
        // Текущий ItemGiver выдаёт синхронно. Успех проверяется по фактическому событию,
        // поскольку TryGiveItem может принять запрос, но не создать игровую сущность.
        var given = equipment.Events.Items.Given;
        given.Hook(OnGiven);
        try { return equipment.TryGiveItem(player, key) && granted; }
        finally { given.Unhook(OnGiven); }
    }

    internal static T Weighted<T>(IReadOnlyList<T> values, Func<T, int> weight)
    {
        var random = Random.Shared.Next(values.Sum(weight));
        foreach (var item in values) { random -= weight(item); if (random < 0) return item; }
        return values[^1];
    }
}
