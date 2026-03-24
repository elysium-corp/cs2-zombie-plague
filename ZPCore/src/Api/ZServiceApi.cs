using SwiftlyS2.Shared.Players;
using ZPApi;
using ZPApi.Events;
using ZPCore.Data.Extensions;

namespace ZPCore.Api;

public sealed class ZServiceApi(IEventSubscriber eventSubscriber) : IZServiceApi
{
    public IEventSubscriber EventSubscriber => eventSubscriber;

    public bool IsInfected(IPlayer player) => player.IsInfected();
}