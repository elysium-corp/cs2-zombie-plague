using Common.Hooks;
using Common.Hooks.Abstractions;
using Common.Math;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Utils.Helpers;
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
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Dictionary<int, CancellationTokenSource> _pending = [];
    private readonly Lock _pendingLock = new();

    public bool TrySetup(IPlayer player, LaserMine mine)
    {
        if (_shutdown.IsCancellationRequested || !CanUseMine(player, mine)) return false;

        var settings = mine.Settings;

        var playerId = player.PlayerID;

        lock (_pendingLock) if (_pending.ContainsKey(playerId)) return false;
        var pawn = player.PlayerPawn;

        if (pawn == null || !pawn.IsValid) return false;

        var gameRules = core.EntitySystem.GetGameRules();

        if (gameRules != null && gameRules.WarmupPeriod) return false;
        if (!EntityPlacer.CanAttachToGround(pawn, settings.MaxDistanceToAttach)) return false;

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        lock (_pendingLock) _pending[playerId] = cancellation;
        _ = SetupAsync(player, playerId, mine, settings, cancellation);
        return true;
    }

    public void Cancel(IPlayer player)
    {
        var cancelled = false;

        lock (_pendingLock)
        {
            if (_pending.TryGetValue(player.PlayerID, out var cancellation))
            {
                cancellation.Cancel();
                cancelled = true;
            }
        }

        if (cancelled && player.IsValid)
        {
            core.MenusAPI.CloseActiveMenu(player);
        }
    }

    private async Task SetupAsync(
        IPlayer player,
        int playerId,
        LaserMine mine,
        LaserMineSettings settings,
        CancellationTokenSource cancellation
    )
    {
        try
        {
            var progress = 0f;
            var window = CreateSetupWindow(player, () => progress, settings.UpdateIntervalMs);

            core.MenusAPI.OpenMenuForPlayer(player, window);

            while (progress < 1f)
            {
                await Task.Delay(settings.UpdateIntervalMs, cancellation.Token).ConfigureAwait(false);

                if (!CanUseMine(player, mine)) return;

                progress = Math.Clamp(
                    progress + settings.UpdateIntervalMs / 1000f / settings.SetupDuration,
                    0f,
                    1f
                );
            }

            await Task.Delay(500, cancellation.Token).ConfigureAwait(false);

            if (!CanUseMine(player, mine) || cancellation.IsCancellationRequested) return;
            var token = cancellation.Token;
            core.Scheduler.NextTick(() =>
            {
                if (token.IsCancellationRequested || !CanUseMine(player, mine)) return;
                core.MenusAPI.CloseActiveMenu(player);
                Spawn(player, mine, settings);
            });
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally
        {
            lock (_pendingLock)
                if (_pending.TryGetValue(playerId, out var current) && ReferenceEquals(current, cancellation))
                    _pending.Remove(playerId);
            cancellation.Dispose();
        }
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

        if (!EntityPlacer.CanAttachToGround(pawn, settings.MaxDistanceToAttach))
        {
            DispatchPlacementRejected(player, null, MinePlacementRejectionReason.InvalidSurface);
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

        if (preContext.Player.PlayerPawn is not { } preparedPawn ||
            !EntityPlacer.CanAttachToGround(preparedPawn, settings.MaxDistanceToAttach))
        {
            entity.Dispose();
            DispatchPlacementRejected(preContext.Player, entity, MinePlacementRejectionReason.InvalidSurface);
            return;
        }

        entity.Spawn(preContext.Player);

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
        lock (_pendingLock)
        {
            _shutdown.Cancel();

            foreach (var cancellation in _pending.Values)
            {
                cancellation.Cancel();
            }

            _pending.Clear();
        }

        _shutdown.Dispose();
    }

    private void DispatchPlacementRejected(
        IPlayer player,
        LaserMineEntityBase? mine,
        MinePlacementRejectionReason reason
    )
    {
        var context = new MinePlacementRejectedContext(player, mine, reason);
        hooks.Dispatch(ref context);
    }
}
