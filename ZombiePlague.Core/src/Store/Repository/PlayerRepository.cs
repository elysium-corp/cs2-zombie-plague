using Common.Database.Storages;
using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Store.Repository;

internal sealed class PlayerRepository(
    PlayerSessionStore<PlayerPreferences> sessions,
    IHookPublisher hooks
) : IPlayerRepository
{
    public string GetZClassId(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return sessions
            .Get(player.SteamID)?
            .Read(data => data.ZClassId)
            ?? PlayerPreferences.DefaultZombieClassId;
    }

    public string GetHClassId(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return sessions
            .Get(player.SteamID)?
            .Read(data => data.HClassId)
            ?? PlayerPreferences.DefaultHumanClassId;
    }

    public void SetZClassId(IPlayer player, string classId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);

        SetClassId(player, classId, PlayerClassKind.Zombie);
    }

    public void SetHClassId(IPlayer player, string classId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);

        SetClassId(player, classId, PlayerClassKind.Human);
    }

    private void SetClassId(IPlayer player, string classId, PlayerClassKind kind)
    {
        var preContext = new ClassSelectingContext(player, classId, kind);

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            DispatchSelectionRejected(preContext, ClassSelectionRejectionReason.Cancelled);
            return;
        }

        if (string.IsNullOrWhiteSpace(preContext.ClassId))
        {
            DispatchSelectionRejected(preContext, ClassSelectionRejectionReason.InvalidClassId);
            return;
        }

        var session = sessions.Get(preContext.Player.SteamID);

        if (session is null)
        {
            DispatchSelectionRejected(preContext, ClassSelectionRejectionReason.SessionUnavailable);
            return;
        }

        session.Update(data =>
        {
            if (kind == PlayerClassKind.Zombie)
            {
                data.ZClassId = preContext.ClassId;
            }
            else
            {
                data.HClassId = preContext.ClassId;
            }
        });

        var postContext = new ClassSelectedContext(preContext.Player, preContext.ClassId, kind);
        hooks.Dispatch(ref postContext);
    }

    private void DispatchSelectionRejected(
        ClassSelectingContext selection,
        ClassSelectionRejectionReason reason
    )
    {
        var context = new ClassSelectionRejectedContext(
            selection.Player,
            selection.ClassId,
            selection.Kind,
            reason
        );

        hooks.Dispatch(ref context);
    }
}
