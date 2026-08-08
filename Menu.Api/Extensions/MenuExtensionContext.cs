using Menu.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Extensions;

public sealed class MenuExtensionContext(IPlayer player, MenuOptionsCollection options )
{
    public IPlayer Player { get; } = player;

    public MenuOptionsCollection Options { get; } = options;
}