using Common.Hooks;
using Common.Hooks.Abstractions;
using Common.Math;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;
using CustomEquipment.Utils.Helpers;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

public sealed class LaserMineInstallerService(
    ISwiftlyCore core,
    IHookPublisher hooks) : ILaserMineInstallerService, IDisposable
{
    private const float MaxDistanceToAttach = 100f;
    private const float SetupDuration = 1.0f;
    private const int UpdateIntervalMs = 100;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Dictionary<int, CancellationTokenSource> _pending = [];
    private readonly Lock _pendingLock = new();

    public bool TrySetup(IPlayer player, LaserMine mine)
    {
        if (_shutdown.IsCancellationRequested || !CanUseMine(player)) return false;

        var playerId = player.PlayerID;

        lock (_pendingLock) if (_pending.ContainsKey(playerId)) return false;
        var pawn = player.PlayerPawn;

        if (pawn == null || !pawn.IsValid) return false;

        var gameRules = core.EntitySystem.GetGameRules();

        if (gameRules != null && gameRules.WarmupPeriod) return false;
        if (!EntityPlacer.CanAttachToGround(pawn, MaxDistanceToAttach)) return false;

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        lock (_pendingLock) _pending[playerId] = cancellation;
        _ = SetupAsync(player, playerId, mine, cancellation);
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
        CancellationTokenSource cancellation
    )
    {
        try
        {
            var progress = 0f;
            var window = CreateSetupWindow(() => progress);

            core.MenusAPI.OpenMenuForPlayer(player, window);

            while (progress < 1f)
            {
                await Task.Delay(UpdateIntervalMs, cancellation.Token).ConfigureAwait(false);

                if (!CanUseMine(player)) return;

                progress = Math.Clamp(progress + UpdateIntervalMs / 1000f / SetupDuration, 0f, 1f);
            }

            await Task.Delay(500, cancellation.Token).ConfigureAwait(false);

            if (!CanUseMine(player) || cancellation.IsCancellationRequested) return;
            var token = cancellation.Token;
            core.Scheduler.NextTick(() =>
            {
                if (token.IsCancellationRequested || !CanUseMine(player)) return;
                core.MenusAPI.CloseActiveMenu(player);
                Spawn(player, mine);
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

    private IMenuAPI CreateSetupWindow(Func<float> getProgress)
    {
        var progressBar = new ProgressBarMenuOption(
            "Установка...",
            getProgress,
            multiLine: false,
            showPercentage: true,
            filledChar: "█",
            emptyChar: "░",
            updateIntervalMs: UpdateIntervalMs
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

    private void Spawn(IPlayer player, LaserMine mine)
    {
        var pawn = player.PlayerPawn;

        if (!CanUseMine(player) || pawn == null || !pawn.IsValid)
        {
            DispatchPlacementRejected(player, null, MinePlacementRejectionReason.InvalidPlayer);
            return;
        }

        if (!EntityPlacer.CanAttachToGround(pawn, MaxDistanceToAttach))
        {
            DispatchPlacementRejected(player, null, MinePlacementRejectionReason.InvalidSurface);
            return;
        }

        var entity = new LaserMineEntity(core);
        var preContext = new MinePlacingContext(player, entity);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            entity.Dispose();
            DispatchPlacementRejected(player, entity, MinePlacementRejectionReason.Cancelled);
            return;
        }

        if (!CanUseMine(preContext.Player))
        {
            entity.Dispose();
            DispatchPlacementRejected(preContext.Player, entity, MinePlacementRejectionReason.InvalidPlayer);
            return;
        }

        if (preContext.Player.PlayerPawn is not { } preparedPawn ||
            !EntityPlacer.CanAttachToGround(preparedPawn, MaxDistanceToAttach))
        {
            entity.Dispose();
            DispatchPlacementRejected(preContext.Player, entity, MinePlacementRejectionReason.InvalidSurface);
            return;
        }

        entity.Spawn(preContext.Player);

        var postContext = new MinePlacedContext(preContext.Player, entity);
        hooks.Dispatch(ref postContext);
    }

    private static bool CanUseMine(IPlayer player)
    {
        if (player is not { IsValid: true, IsAlive: true })
        {
            return false;
        }

        var pawn = player.PlayerPawn;

        return pawn is { IsValid: true } && pawn.Team == Team.CT;
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
