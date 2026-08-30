using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Api.Results;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Swiftly;

internal sealed record MenuRenderRequest
{
    internal required IPlayer Caller { get; init; }
    internal required IPlayer Target { get; init; }
    internal required MenuDefinition Menu { get; init; }
    internal required MenuAudienceDefinition InvocationAudience { get; init; }
    internal required string Locale { get; init; }
    internal required int Depth { get; init; }
    internal required Func<MenuItemDefinition?, MenuActionDefinition, JsonElement?, MenuOperationResult> Execute { get; init; }
    internal required Func<MenuActionDefinition, bool> IsActionAvailable { get; init; }
    internal Func<MenuReferenceDefinition, int, IMenuAPI?>? BuildParent { get; init; }
}
