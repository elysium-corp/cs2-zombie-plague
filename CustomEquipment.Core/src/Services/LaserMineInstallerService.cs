using Common.Hooks;
using Common.Hooks.Abstractions;
using Common.Math;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;
using CustomEquipment.Utils.Helpers;
using Economy.Api;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

public sealed class LaserMineInstallerService(
    ISwiftlyCore core,
    IHookPublisher hooks,
    IEconomyApi economyApi) : ILaserMineInstallerService, IDisposable
{
    private const float MaxDistanceToAttach = 100f;
    private const float SetupDuration = 1.0f;
    private const int UpdateIntervalMs = 100;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Dictionary<int, CancellationTokenSource> _pending = [];
    private readonly Lock _pendingLock = new();

    public bool TrySetup(IPlayer player, LaserMine mine)
    {
        if (_shutdown.IsCancellationRequested) return false;
        lock (_pendingLock) if (_pending.ContainsKey(player.PlayerID)) return false;
        var pawn = player.PlayerPawn;

        if (pawn == null || !pawn.IsValid) return false;
        if (pawn.Team == Team.T) return false;

        var gameRules = core.EntitySystem.GetGameRules();

        if (gameRules != null && gameRules.WarmupPeriod) return false;
        if (!EntityPlacer.CanAttachToGround(pawn, MaxDistanceToAttach)) return false;

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        lock (_pendingLock) _pending[player.PlayerID] = cancellation;
        _ = SetupAsync(player, mine, cancellation);
        return true;
    }

    private async Task SetupAsync(IPlayer player, LaserMine mine, CancellationTokenSource cancellation)
    {
        try
        {
            var progress = 0f;
            var window = CreateSetupWindow(() => progress);

            core.MenusAPI.OpenMenuForPlayer(player, window);

            while (progress < 1f)
            {
                await Task.Delay(UpdateIntervalMs, cancellation.Token).ConfigureAwait(false);

                if (!player.IsValid) return;

                progress = Math.Clamp(progress + UpdateIntervalMs / 1000f / SetupDuration, 0f, 1f);
            }

            await Task.Delay(500, cancellation.Token).ConfigureAwait(false);

            if (!player.IsValid || cancellation.IsCancellationRequested) return;
            var token = cancellation.Token;
            core.Scheduler.NextTick(() =>
            {
                if (token.IsCancellationRequested || !player.IsValid) return;
                core.MenusAPI.CloseActiveMenu(player);
                Spawn(player, mine);
            });
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally
        {
            lock (_pendingLock)
                if (_pending.TryGetValue(player.PlayerID, out var current) && ReferenceEquals(current, cancellation))
                    _pending.Remove(player.PlayerID);
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

        if (pawn == null || !pawn.IsValid ||
            !EntityPlacer.CanAttachToGround(pawn, MaxDistanceToAttach))
        {
            economyApi.GiveMoney(player, mine.Price.Item);
            DispatchPlacementRejected(player, null, MinePlacementRejectionReason.InvalidSurface);
            return;
        }

        var entity = new LaserMineEntity(core);
        var preContext = new MinePlacingContext(player, entity);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            entity.Dispose();
            economyApi.GiveMoney(player, mine.Price.Item);
            DispatchPlacementRejected(player, entity, MinePlacementRejectionReason.Cancelled);
            return;
        }

        if (!preContext.Player.IsValid)
        {
            entity.Dispose();
            economyApi.GiveMoney(player, mine.Price.Item);
            DispatchPlacementRejected(preContext.Player, entity, MinePlacementRejectionReason.InvalidPlayer);
            return;
        }

        entity.Spawn(preContext.Player);

        var postContext = new MinePlacedContext(preContext.Player, entity);
        hooks.Dispatch(ref postContext);
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        CancellationTokenSource[] pending;
        lock (_pendingLock) { pending = _pending.Values.ToArray(); _pending.Clear(); }
        foreach (var cancellation in pending) cancellation.Cancel();
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
