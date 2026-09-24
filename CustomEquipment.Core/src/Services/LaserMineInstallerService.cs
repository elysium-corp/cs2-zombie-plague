using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Utils;
using CustomEquipment.Utils.Helpers;
using Microsoft.Extensions.Logging;
using Localization.Api;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

public sealed class LaserMineInstallerService(
    ISwiftlyCore core,
    IHookPublisher hooks,
    ILocalizationApi localization) : ILaserMineInstallerService, IDisposable
{
    private readonly Dictionary<int, PendingInstallation> _pending = [];
    private bool _disposed;

    public bool TrySetup(IPlayer player, LaserMine mine)
    {
        if (_disposed) return false;
        if (!CanUseMine(player, mine) || _pending.ContainsKey(player.PlayerID))
        {
            core.Logger.LogInformation(
                "[LaserMine] Установка недоступна: player={Player}, pending={Pending}.",
                player.PlayerID, _pending.ContainsKey(player.PlayerID));
            return false;
        }

        var settings = mine.Settings;
        var pawn = player.PlayerPawn!;
        var gameRules = core.EntitySystem.GetGameRules();
        if (gameRules is { WarmupPeriod: true })
        {
            core.Logger.LogInformation("[LaserMine] Установка недоступна во время разминки: player={Player}.", player.PlayerID);
            return false;
        }
        if (!EntityPlacer.CanAttachToGround(core, pawn, settings.MaxDistanceToAttach)) return false;

        var pending = new PendingInstallation(core.EntitySystem.GetRefEHandle(pawn).Raw);
        _pending.Add(player.PlayerID, pending);
        try
        {
            var window = CreateSetupWindow(player, () => pending.Progress, settings.UpdateIntervalMs);
            core.MenusAPI.OpenMenuForPlayer(player, window);

            var interval = settings.UpdateIntervalMs / 1000f;
            // Проверка игрока, меню, звуки и создание сущностей выполняются в игровом потоке.
            pending.Timer = core.Scheduler.DelayAndRepeatBySeconds(interval, interval, () =>
            {
                if (_disposed || !_pending.TryGetValue(player.PlayerID, out var current) ||
                    !ReferenceEquals(current, pending)) return;

                try
                {
                    if (!CanUseMine(player, mine) ||
                        core.EntitySystem.GetRefEHandle(player.PlayerPawn!).Raw != pending.PawnHandle)
                    {
                        Cancel(player);
                        return;
                    }

                    pending.Progress = Math.Clamp(pending.Progress + interval / settings.SetupDuration, 0f, 1f);
                    if (pending.Progress < 1f) return;
                    Cancel(player);
                    Spawn(player, mine, settings);
                }
                catch (Exception exception)
                {
                    Cancel(player);
                    core.Logger.LogError(exception, "[LaserMine] Не удалось завершить установку мины.");
                }
            });
            core.Scheduler.StopOnMapChange(pending.Timer);
            return true;
        }
        catch
        {
            Cancel(player);
            throw;
        }
    }

    public void Cancel(IPlayer player)
    {
        if (!_pending.Remove(player.PlayerID, out var pending)) return;
        pending.Cancel();
        if (player.IsValid) core.MenusAPI.CloseActiveMenu(player);
    }

    public void CancelAll()
    {
        foreach (var player in core.PlayerManager.GetAllPlayers()) Cancel(player);
        foreach (var pending in _pending.Values) pending.Cancel();
        _pending.Clear();
    }

    private IMenuAPI CreateSetupWindow(IPlayer player, Func<float> getProgress, int updateIntervalMs)
    {
        var progressBar = new ProgressBarMenuOption(
            localization.GetForPlayerOrKey(player, "Equipment.LaserMine.Installing"),
            getProgress,
            multiLine: false,
            showPercentage: true,
            filledChar: "█",
            emptyChar: "░",
            updateIntervalMs: updateIntervalMs
        );

        return core.MenusAPI.CreateBuilder()
            .DisableExit()
            .DisableSound()
            .SetAutoCloseDelay()
            .Design.SetMenuTitleVisible(false)
            .Design.SetMenuFooterVisible(false)
            .Design.SetMaxVisibleItems(1)
            .AddOption(progressBar)
            .Build();
    }

    private void Spawn(IPlayer player, LaserMine mine, LaserMineSettings settings)
    {
        var pawn = player.PlayerPawn;

        if (!CanUseMine(player, mine) || pawn == null || !pawn.IsValid)
        {
            DispatchPlacementRejected(player, null, MinePlacementRejectionReason.InvalidPlayer);
            return;
        }

        var entity = new LaserMineEntity(core, settings);
        var preContext = new MinePlacingContext(player, entity);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            entity.Dispose();
            DispatchPlacementRejected(player, entity, MinePlacementRejectionReason.Cancelled);
            return;
        }

        if (!CanUseMine(preContext.Player, mine))
        {
            entity.Dispose();
            DispatchPlacementRejected(preContext.Player, entity, MinePlacementRejectionReason.InvalidPlayer);
            return;
        }

        if (!entity.TrySpawn(preContext.Player, settings.MaxDistanceToAttach))
        {
            entity.Dispose();
            DispatchPlacementRejected(preContext.Player, entity, MinePlacementRejectionReason.InvalidSurface);
            return;
        }

        var postContext = new MinePlacedContext(preContext.Player, entity);
        hooks.Dispatch(ref postContext);
    }

    private static bool CanUseMine(IPlayer player, LaserMine mine)
    {
        if (player is not { IsValid: true, IsAlive: true } ||
            mine is IManagedGameplayItem { Enabled: false })
        {
            return false;
        }

        var pawn = player.PlayerPawn;

        if (pawn is not { IsValid: true })
        {
            return false;
        }

        var playerAccess = pawn.Team switch
        {
            Team.CT => AccessFlags.Human,
            Team.T => AccessFlags.Zombie,
            _ => AccessFlags.None
        };

        return (mine.AccessFlags & playerAccess) != 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelAll();
    }

    private sealed class PendingInstallation(uint pawnHandle)
    {
        public uint PawnHandle { get; } = pawnHandle;
        public float Progress { get; set; }
        public CancellationTokenSource? Timer { get; set; }

        public void Cancel()
        {
            try
            {
                Timer?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Планировщик мог освободить таймер при смене карты.
            }
        }
    }

    private void DispatchPlacementRejected(
        IPlayer player,
        LaserMineEntityBase? mine,
        MinePlacementRejectionReason reason
    )
    {
        var context = new MinePlacementRejectedContext(player, mine, reason);
        hooks.Dispatch(ref context);
        if (reason == MinePlacementRejectionReason.InvalidSurface && player.IsValid)
        {
            player.SendAlert(localization.GetForPlayerOrKey(player, "Equipment.LaserMine.InvalidSurface"));
        }
    }
}
