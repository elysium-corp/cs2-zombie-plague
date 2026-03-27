using Common.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Utils;

public static class Localizer
{
    public static string GetLocalizeString(IPlayer player, string key)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var localizer = core.Translation.GetPlayerLocalizer(player);
        
        return localizer[$"CustomKnife.{key}"];
    }
}