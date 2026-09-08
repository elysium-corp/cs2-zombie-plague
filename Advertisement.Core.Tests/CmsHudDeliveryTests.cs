using System.Collections.Frozen;
using System.Reflection;
using Advertisement.Core.Application;
using Advertisement.Core.Data;
using Advertisement.Core.Database;
using Advertisement.Core.Database.Entities;
using Advertisement.Core.Database.Migrations;
using CustomHud.Api;
using Localization.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using SwiftlyS2.Shared.Players;

namespace Advertisement.Core.Tests;

public sealed class CmsHudDeliveryTests
{
    [Theory]
    [InlineData("chat", false, 0)]
    [InlineData("hud", true, 1)]
    [InlineData("chat_and_hud", false, 1)]
    public void CmsSettingsOverrideLegacyConfig(string mode, bool suppressChat, int calls)
    {
        var api = new Hud();
        using var delivery = Delivery(api);
        Assert.Equal(suppressChat, delivery.Send(null!, "Test", () => "HUD", new(mode, "Hud.Text", "top_center", 12, "banner")));
        Assert.Equal(calls, api.Texts.Count);
        if (calls == 0) return;
        Assert.Equal(HudPosition.TopCenter, api.Last!.Position);
        Assert.Equal(HudMessageStyle.Banner, api.Last.Style);
        Assert.Equal(12, api.Last.DurationSeconds);
    }

    [Fact]
    public void HudOnlyNeverLeaksIntoChatOnInvalidSettingsOrMissingText()
    {
        var api = new Hud();
        using var delivery = Delivery(api);
        foreach (var presentation in new AdvertisementPresentation[]
        {
            new("invalid", "Hud.Text"), new("hud", "Hud.Text", "invalid"),
            new("hud", "Hud.Text", HudDurationSeconds: double.NaN),
            new("hud", "Hud.Text", HudDurationSeconds: 61), new("hud", "Hud.Text", HudStyle: "invalid")
        }) Assert.Equal(presentation.DisplayType == "hud", delivery.Send(null!, "Test", () => throw new InvalidOperationException(), presentation));
        Assert.True(delivery.Send(null!, "Test", () => null, new("hud", "Missing")));
        Assert.Empty(api.Texts);
    }

    [Theory]
    [InlineData("chat", true)]
    [InlineData("hud", true)]
    [InlineData("chat_and_hud", true)]
    [InlineData("hud", false)]
    public void SenderSeparatesTemplatesAndEscapesHudParameters(string mode, bool available)
    {
        var chat = new List<string>();
        var player = Proxy<IPlayer>((method, args) => method.Name switch
        {
            "get_IsAuthorized" => true, "get_IsFakeClient" => false,
            "get_Name" => "<b>[red]Игрок", "get_SteamID" => 76561198000000001UL,
            "SendMessage" => Capture(chat, args!), _ => null
        });
        var localization = Proxy<ILocalizationApi>((method, args) =>
        {
            if (!method.Name.StartsWith("FormatFor", StringComparison.Ordinal)) return null;
            var key = (string)args![1]!;
            var parameters = (IReadOnlyDictionary<string, object?>)args[2]!;
            return key == "Chat.Text" ? "Chat only" : $"<b>{parameters["player_name"]}</b>";
        });
        var api = new Hud { Available = available };
        using var delivery = Delivery(api);
        var message = DatabaseAdvertisementProvider.MapMessage(new AdvertisementMessageEntity
        {
            Id = 1, Key = "Test", Name = "Test", LocalizationKey = "Chat.Text",
            DisplayType = mode, HudLocalizationKey = "Hud.Text"
        });
        var snapshot = new AdvertisementSnapshot(new(true, 90, 30, 0, AdvertisementOrderMode.Sequential, true, 1),
            new[] { message }.ToFrozenDictionary(item => item.Id), DateTimeOffset.UtcNow, AdvertisementSource.Database);
        new AdvertisementSender(() => localization, delivery).Send(snapshot, message, [player],
            1, 0, "Server", "Map", "Next", 32, DateTimeOffset.UtcNow, null);
        Assert.Equal(mode == "hud" ? 0 : 1, chat.Count);
        if (chat.Count > 0) Assert.Contains("Chat only", chat[0]);
        Assert.Equal(mode != "chat" && available ? 1 : 0, api.Texts.Count);
        if (api.Texts.Count > 0) Assert.Equal("<b>&lt;b&gt;&#91;red&#93;Игрок</b>", api.Texts[0]);
    }

    [Fact]
    public void ChatDoesNotInterpretHtmlOrEncodedChatControls()
    {
        Assert.Equal("Привет мир [red]цвет[/] &#91;blue&#93;", AdvertisementChatText.Normalize(
            "<b>Привет</b><br>мир [red]цвет[/] &#91;blue&#93;"));
    }

    [Fact]
    public async Task MigrationPreservesChatAndEnforcesHudContractInPostgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("ADVERTISEMENT_TEST_POSTGRES");
        if (connectionString is null) return;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        async Task Execute(string sql) => await new NpgsqlCommand(sql, connection, transaction).ExecuteNonQueryAsync();
        await Execute("""
            CREATE SCHEMA advertisement;
            CREATE SCHEMA localization;
            CREATE TABLE localization.entries(key VARCHAR(191) PRIMARY KEY);
            INSERT INTO localization.entries VALUES ('Hud.Text');
            CREATE TABLE advertisement.settings(configuration_version BIGINT, updated_at TIMESTAMPTZ);
            INSERT INTO advertisement.settings VALUES (1, NOW());
            CREATE TABLE advertisement.messages(id BIGINT PRIMARY KEY, display_type VARCHAR(32) NOT NULL DEFAULT 'chat' CHECK (display_type = 'chat'));
            INSERT INTO advertisement.messages(id) VALUES (1);
            """);
        var migration = new AddHudDelivery();
        foreach (var operation in migration.UpOperations.Cast<SqlOperation>()) await Execute(operation.Sql);
        await Execute("UPDATE advertisement.messages SET display_type = 'chat_and_hud', hud_localization_key = 'Hud.Text', hud_position = 'top_center', hud_style = 'banner'");
        foreach (var invalid in new[] { "hud_localization_key = NULL", "hud_position = 'invalid'", "hud_duration_seconds = 'NaN'", "hud_duration_seconds = 61", "hud_style = 'invalid'" })
        {
            await Execute("SAVEPOINT invalid_setting");
            await Assert.ThrowsAsync<PostgresException>(() => Execute("UPDATE advertisement.messages SET " + invalid));
            await Execute("ROLLBACK TO SAVEPOINT invalid_setting");
        }
        foreach (var operation in migration.DownOperations.Cast<SqlOperation>()) await Execute(operation.Sql);
        var mode = await new NpgsqlCommand("SELECT display_type FROM advertisement.messages WHERE id = 1", connection, transaction).ExecuteScalarAsync();
        Assert.Equal("chat", mode);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task BannerMigrationKeepsHudOnlyWithoutChatAndProtectsReferencesAndRollback()
    {
        var connectionString = Environment.GetEnvironmentVariable("ADVERTISEMENT_TEST_POSTGRES");
        if (connectionString is null) return;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        async Task Execute(string sql) => await new NpgsqlCommand(sql, connection, transaction).ExecuteNonQueryAsync();
        await Execute("""
            CREATE SCHEMA advertisement;
            CREATE SCHEMA localization;
            CREATE TABLE localization.entries(key VARCHAR(191) PRIMARY KEY);
            INSERT INTO localization.entries VALUES ('Chat.Text'), ('Hud.Text'), ('Hud.Title');
            CREATE TABLE advertisement.settings(configuration_version BIGINT, updated_at TIMESTAMPTZ);
            INSERT INTO advertisement.settings VALUES (1, NOW());
            CREATE TABLE advertisement.messages(id BIGINT PRIMARY KEY, localization_key VARCHAR(191) NOT NULL REFERENCES localization.entries(key), display_type VARCHAR(32) NOT NULL DEFAULT 'chat');
            INSERT INTO advertisement.messages(id, localization_key) VALUES (1, 'Chat.Text');
            """);
        foreach (var operation in new AddHudDelivery().UpOperations.Cast<SqlOperation>()) await Execute(operation.Sql);
        var migration = new AddBannerTemplates();
        foreach (var operation in migration.UpOperations.Cast<SqlOperation>()) await Execute(operation.Sql);
        await Execute("""
            INSERT INTO advertisement.banner_templates(key, name, design) VALUES ('Round.Start', 'Round', '{"SchemaVersion":1,"Variant":"headline"}');
            UPDATE advertisement.messages SET display_type = 'hud', localization_key = NULL, hud_localization_key = 'Hud.Text', banner_template_key = 'Round.Start', banner_title_key = 'Hud.Title', banner_parameters = '{"reward":"250"}';
            """);
        Assert.Equal(4L, await new NpgsqlCommand("SELECT configuration_version FROM advertisement.settings", connection, transaction).ExecuteScalarAsync());
        foreach (var invalid in new[]
        {
            "DELETE FROM advertisement.banner_templates",
            "DELETE FROM localization.entries WHERE key = 'Hud.Title'",
            "UPDATE advertisement.messages SET display_type = 'chat'",
            "UPDATE advertisement.messages SET banner_parameters = '[]'",
            "UPDATE advertisement.messages SET banner_template_key = NULL"
        })
        {
            await Execute("SAVEPOINT rejected");
            await Assert.ThrowsAsync<PostgresException>(() => Execute(invalid));
            await Execute("ROLLBACK TO SAVEPOINT rejected");
        }
        await Execute("SAVEPOINT rollback_guard");
        await Assert.ThrowsAsync<PostgresException>(() => Execute(migration.DownOperations.Cast<SqlOperation>().Single().Sql));
        await Execute("ROLLBACK TO SAVEPOINT rollback_guard");
        await Execute("UPDATE advertisement.messages SET localization_key = 'Chat.Text'");
        foreach (var operation in migration.DownOperations.Cast<SqlOperation>()) await Execute(operation.Sql);
        Assert.Equal("hud", await new NpgsqlCommand("SELECT display_type FROM advertisement.messages", connection, transaction).ExecuteScalarAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task NotificationMigrationsSeedLocalizedEventsAndProtectConfiguredBindings()
    {
        var connectionString = Environment.GetEnvironmentVariable("ADVERTISEMENT_TEST_POSTGRES");
        if (connectionString is null) return;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        async Task Execute(string sql) => await new NpgsqlCommand(sql, connection, transaction).ExecuteNonQueryAsync();
        async Task<object?> Scalar(string sql) => await new NpgsqlCommand(sql, connection, transaction).ExecuteScalarAsync();
        await Execute("""
            CREATE SCHEMA advertisement;
            CREATE SCHEMA localization;
            CREATE TABLE localization.entries(id BIGSERIAL PRIMARY KEY, key VARCHAR(191) UNIQUE NOT NULL, description TEXT, parameters JSONB, is_critical BOOLEAN);
            CREATE TABLE localization.languages(code TEXT PRIMARY KEY);
            INSERT INTO localization.languages VALUES ('ru'), ('en'), ('de'), ('pl');
            CREATE TABLE localization.translations(entry_id BIGINT REFERENCES localization.entries(id), language_code TEXT REFERENCES localization.languages(code), text TEXT, PRIMARY KEY(entry_id, language_code));
            CREATE TABLE localization.settings(configuration_version BIGINT, updated_at TIMESTAMPTZ);
            INSERT INTO localization.settings VALUES (1, NOW());
            CREATE TABLE advertisement.settings(configuration_version BIGINT, updated_at TIMESTAMPTZ);
            INSERT INTO advertisement.settings VALUES (1, NOW());
            CREATE TABLE advertisement.messages(id BIGINT PRIMARY KEY, localization_key VARCHAR(191) NOT NULL REFERENCES localization.entries(key), display_type VARCHAR(32) NOT NULL DEFAULT 'chat');
            """);
        // Выполняем саму миграцию Localization, затем миграции зависимого модуля.
        var localizationType = Assembly.Load("Localization.Core").GetType("Localization.Core.Database.Migrations.AddNotificationLocalization", true)!;
        var localizationMigration = (Microsoft.EntityFrameworkCore.Migrations.Migration)Activator.CreateInstance(localizationType, nonPublic: true)!;
        foreach (var migration in new Microsoft.EntityFrameworkCore.Migrations.Migration[]
                 { localizationMigration, new AddHudDelivery(), new AddBannerTemplates(), new AddNotificationRules() })
            foreach (var operation in migration.UpOperations.Cast<SqlOperation>()) await Execute(operation.Sql);
        Assert.Equal((long)NotificationCatalog.Defaults.Count, await Scalar("SELECT COUNT(*) FROM advertisement.notification_rules"));
        Assert.Equal((long)NotificationCatalog.Defaults.Count, await Scalar("SELECT COUNT(*) FROM localization.entries"));
        Assert.Equal("top_left", await Scalar("SELECT settings->>'Position' FROM advertisement.hud_widgets WHERE key = 'ZombiePlague.Abilities'"));
        var version = (long)(await Scalar("SELECT configuration_version FROM advertisement.settings"))!;
        await Execute("UPDATE advertisement.hud_widgets SET settings = jsonb_set(settings, '{ScalePercent}', '75')");
        Assert.Equal(version + 1, await Scalar("SELECT configuration_version FROM advertisement.settings"));
        await Execute("""
            INSERT INTO advertisement.banner_templates(key, name, design) VALUES ('Custom.Title', 'Title', '{"SchemaVersion":1,"Variant":"custom","ShowHeader":false,"ShowTitle":true,"ShowDescription":false}');
            UPDATE advertisement.notification_rules SET template_key = 'Custom.Title', title_key = description_key, description_key = NULL WHERE event_key = 'Game.Round.Started';
            INSERT INTO advertisement.messages(id, display_type, banner_template_key, banner_title_key) VALUES (1, 'hud', 'Custom.Title', 'Notifications.Game.Round.Started');
            """);
        foreach (var invalid in new[]
        {
            "DELETE FROM advertisement.banner_templates WHERE key = 'Custom.Title'",
            "DELETE FROM localization.entries WHERE key = 'Notifications.Game.Round.Started'",
            "UPDATE advertisement.notification_rules SET description_key = 'Missing.Text'",
            "UPDATE advertisement.notification_rules SET settings = '[]'",
            "UPDATE advertisement.hud_widgets SET settings = '[]'",
            "UPDATE advertisement.messages SET banner_title_key = NULL"
        })
        {
            await Execute("SAVEPOINT rejected_notification");
            await Assert.ThrowsAsync<PostgresException>(() => Execute(invalid));
            await Execute("ROLLBACK TO SAVEPOINT rejected_notification");
        }
        await Execute("SAVEPOINT rollback_guard");
        await Assert.ThrowsAsync<PostgresException>(() => Execute(new AddNotificationRules().DownOperations.Cast<SqlOperation>().Single().Sql));
        await Execute("ROLLBACK TO SAVEPOINT rollback_guard");
        await Execute("DELETE FROM advertisement.messages");
        foreach (var operation in new AddNotificationRules().DownOperations.Cast<SqlOperation>()) await Execute(operation.Sql);
        Assert.Equal((long)NotificationCatalog.Defaults.Count, await Scalar("SELECT COUNT(*) FROM localization.entries"));
        await transaction.RollbackAsync();
    }

    [Fact]
    public void TitleOnlyBannerReachesLocalizedApiWithoutAnUnusedDescription()
    {
        HudBannerContent? received = null;
        var banners = Proxy<ICustomBannerApi>((method, args) => method.Name switch
        {
            "get_IsAvailable" => true,
            "ShowLocalized" => (received = (HudBannerContent)args![2]!) is not null,
            _ => null
        });
        var delivery = Delivery(new Hud());
        delivery.Initialize(new Hud(), banners);
        var presentation = new AdvertisementPresentation("hud", null)
        {
            Template = new() { Variant = "custom", ShowHeader = false, ShowDescription = false },
            TitleKey = "Hud.Title", HudLocalizationKey = null
        };
        Assert.True(delivery.Send(Proxy<IPlayer>((_, _) => null), "Title", () => throw new Exception("Description must not be requested"), presentation));
        Assert.Equal("Hud.Title", received?.Title);
        Assert.Null(received?.Description);
    }

    private static object? Capture(List<string> messages, object?[] args)
    {
        messages.Add(args.OfType<string>().Single());
        return null;
    }

    private static AdvertisementHudDelivery Delivery(Hud api)
    {
        var delivery = new AdvertisementHudDelivery(Options.Create(new AdvertisementHudConfig { Mode = AdvertisementDeliveryMode.Hud }), NullLogger.Instance);
        delivery.Initialize(api);
        return delivery;
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        ((InterfaceStub)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        internal Func<MethodInfo, object?[]?, object?> InvokeMethod = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => InvokeMethod(targetMethod!, args);
    }

    private sealed class Hud : ICustomHudApi
    {
        internal bool Available = true;
        internal readonly List<string> Texts = [];
        internal HudMessageOptions? Last;
        public bool IsAvailable => Available;
        public bool Show(IPlayer player, string text, HudMessageOptions? options = null) { Texts.Add(text); Last = options; return true; }
        public int Broadcast(string text, HudMessageOptions? options = null) => throw new NotImplementedException();
        public void Hide(IPlayer player, string channel) { }
        public void ClearChannel(string channel) { }
    }
}
