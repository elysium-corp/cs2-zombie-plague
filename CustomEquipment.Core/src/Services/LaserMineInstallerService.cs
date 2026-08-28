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
    IEconomyApi economyApi) : ILaserMineInstallerService
{
    private const float MaxDistanceToAttach = 100f;
    private const float SetupDuration = 1.0f;
    private const int UpdateIntervalMs = 100;

    public bool TrySetup(IPlayer player, LaserMine mine)
    {
        var pawn = player.PlayerPawn;

        if (pawn == null || !pawn.IsValid) return false;
        if (pawn.Team == Team.T) return false;

        var gameRules = core.EntitySystem.GetGameRules();

        if (gameRules != null && gameRules.WarmupPeriod) return false;
        if (!EntityPlacer.CanAttachToGround(pawn, MaxDistanceToAttach)) return false;

        _ = SetupAsync(player, mine);
        return true;
    }

    private async Task SetupAsync(IPlayer player, LaserMine mine)
    {
        var progress = 0f;
        var window = CreateSetupWindow(() => progress);

        core.MenusAPI.OpenMenuForPlayer(player, window);

        while (progress < 1f)
        {
            await Task.Delay(UpdateIntervalMs);

            if (!player.IsValid)
            {
                return;
            }

            progress = Math.Clamp(
                progress + UpdateIntervalMs / 1000f / SetupDuration,
                0f,
                1f);
        }

        await Task.Delay(500);

        if (!player.IsValid)
        {
            return;
        }

        core.MenusAPI.CloseActiveMenu(player);
        await core.Scheduler.NextTickAsync(() => Spawn(player, mine));
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
            economyApi.GiveMoney(player, mine.Price.Item);
            DispatchPlacementRejected(player, entity, MinePlacementRejectionReason.Cancelled);
            return;
        }

        if (!preContext.Player.IsValid)
        {
            economyApi.GiveMoney(player, mine.Price.Item);
            DispatchPlacementRejected(preContext.Player, entity, MinePlacementRejectionReason.InvalidPlayer);
            return;
        }

        entity.Spawn(preContext.Player);

        var postContext = new MinePlacedContext(preContext.Player, entity);
        hooks.Dispatch(ref postContext);
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
