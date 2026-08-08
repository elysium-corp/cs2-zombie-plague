using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Api.Data.Store;

public interface IPlayerRepository
{
    public string GetZClassId(IPlayer player);

    public string GetHClassId(IPlayer player);

    public string GetKnifeId(IPlayer player);

    public void SetZClassId(IPlayer player, string classId);

    public void SetHClassId(IPlayer player, string classId);

    public void SetKnifeId(IPlayer player, string knifeId);

    public bool Remove(IPlayer player);
}