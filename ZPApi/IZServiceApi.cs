using SwiftlyS2.Shared.Players;
using ZPApi.Events;
using ZPApi.Generated;

namespace ZPApi;

public interface IZServiceApi
{
    public IEventSubscriber EventSubscriber { get; }

    public bool IsInfected(IPlayer player);

    public static readonly string VersionApi = BuildInfo.ApiVersion;

    public static readonly string SharedApiKey = "ZP.Core.IZServiceApi";
}