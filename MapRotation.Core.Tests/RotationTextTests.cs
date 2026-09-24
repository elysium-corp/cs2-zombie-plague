using System.Reflection;
using Localization.Api;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class RotationTextTests
{
    [Fact]
    public void PlayerAndParametersArePassedToTheCentralFormatter()
    {
        var player = Stub<IPlayer>((_, _) => throw new InvalidOperationException("Язык определяет Localization.Core."));
        var api = Stub<ILocalizationApi>((method, args) =>
        {
            Assert.Equal(nameof(ILocalizationApi.FormatForPlayer), method.Name);
            Assert.Same(player, args![0]);
            Assert.Equal("MapRotation.RtvAdded", args[1]);
            var values = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(args[2]);
            Assert.Equal("<Player>[red]", values["player"]);
            Assert.Equal("4", values["votes"]);
            Assert.Equal("7", values["required"]);
            return "Текст из общего каталога";
        });

        Assert.Equal("Текст из общего каталога", new RotationText(() => api).Get(player, "RtvAdded",
            ("player", "<Player>[red]"), ("votes", "4"), ("required", "7")));
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("en")]
    [InlineData("pl")]
    public void ConsoleUsesTheConfiguredServerFallbackLanguage(string language)
    {
        var api = Stub<ILocalizationApi>((method, args) =>
        {
            if (method.Name == "get_ServerFallbackLanguage") return language;
            Assert.Equal(nameof(ILocalizationApi.FormatForLanguage), method.Name);
            Assert.Equal(language, args![0]);
            Assert.Equal("MapRotation.Admin.NextMapSet", args[1]);
            Assert.Equal("Gorodok", Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(args[2])["map"]);
            return "Server translation";
        });

        Assert.Equal("Server translation", new RotationText(() => api).Get(null, "Admin.NextMapSet", ("map", "Gorodok")));
    }

    [Fact]
    public void RebindingUsesTheNewApiAndDoesNotCacheTranslations()
    {
        var player = Stub<IPlayer>((_, _) => throw new InvalidOperationException());
        var first = Stub<ILocalizationApi>((_, _) => "Первый каталог");
        var second = Stub<ILocalizationApi>((_, _) => "Обновлённый каталог");
        var current = first;
        var text = new RotationText(() => current);

        Assert.Equal("Первый каталог", text.Get(player, "Close"));
        current = second;
        Assert.Equal("Обновлённый каталог", text.Get(player, "Close"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingTranslationReturnsTheFullKeyWithoutALocalFallback(bool fromConsole)
    {
        var player = fromConsole ? null : Stub<IPlayer>((_, _) => throw new InvalidOperationException());
        var api = Stub<ILocalizationApi>((method, _) => method.Name == "get_ServerFallbackLanguage" ? "en" : null);

        Assert.Equal("MapRotation.Loading", new RotationText(() => api).Get(player, "Loading"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChatPrefixRespectsTheCentralTagAndItsDisabledState(bool enabled)
    {
        var player = Stub<IPlayer>((_, _) => throw new InvalidOperationException());
        var api = Stub<ILocalizationApi>((method, args) =>
        {
            Assert.Equal(nameof(ILocalizationApi.GetTagForPlayer), method.Name);
            Assert.Same(player, args![0]);
            Assert.Equal("Elysium", args[1]);
            return enabled ? new LocalizationTag("Elysium", "Элизиум", "green") : null;
        });

        Assert.Equal(enabled ? "[green][Элизиум][/] Сообщение" : "Сообщение",
            new RotationText(() => api).WithChatTag(player, "Сообщение"));
    }

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        ((InterfaceStub)(object)proxy).Handler = handler;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
