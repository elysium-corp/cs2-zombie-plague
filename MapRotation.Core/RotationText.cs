using Localization.Api;
using SwiftlyS2.Shared.Players;

namespace MapRotation.Core;

/// <summary>Получает текст из общего каталога, включая язык консоли и префиксы чата.</summary>
internal sealed class RotationText(Func<ILocalizationApi> localization)
{
    public string Get(IPlayer? player, string suffix, params (string Name, string Value)[] parameters)
    {
        var api = localization();
        var key = "MapRotation." + suffix;
        var values = parameters.ToDictionary(parameter => parameter.Name, parameter => (object?)parameter.Value);
        return player is not null
            ? api.FormatForPlayerOrKey(player, key, values)
            : api.FormatForLanguage(api.ServerFallbackLanguage, key, values) ?? key;
    }

    public string WithChatTag(IPlayer player, string message)
    {
        var tag = localization().GetTagForPlayer(player, "Elysium");
        return tag is null ? message : $"[{tag.Color}][{tag.Text}][/] {message}";
    }
}
