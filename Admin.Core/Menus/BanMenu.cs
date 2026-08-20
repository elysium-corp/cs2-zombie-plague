using Admin.Api.Permissions;
using Admin.Core.Services;
using Common.Database.Tasks;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Menus;

internal sealed class BanMenu(
    ISwiftlyCore core, IPrivilegeService privilegeService, IBanService banService,
    IBanEnforcementService banEnforcementService, DatabaseTaskTracker databaseTasks
) : MenuBase(core)
{
    private static readonly string[] PredefinedReasons =
    [
        "Использование читов",
        "Оскорбления / токсичное поведение",
        "Спам / флуд",
        "Помеха игровому процессу",
        "Обход блокировки",
        "Реклама",
        "Нарушение правил сервера"
    ];

    public override string Id => "admin.ban";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return privilegeService.HasPermission(player.SteamID, AdminPermissions.Ban);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var players = Core.PlayerManager
            .GetAllValidPlayers()
            .Where(target => !target.IsFakeClient && target.IsAuthorized && target.SteamID != 0 && target.PlayerID != player.PlayerID)
            .OrderBy(target => target.Controller.PlayerName, StringComparer.OrdinalIgnoreCase);

        foreach (var target in players)
        {
            var banTarget = new BanTarget(
                target.PlayerID,
                target.SteamID,
                target.Controller.PlayerName
            );

            builder.AddOption(BuildPlayerOption(banTarget));
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Забанить игрока")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildPlayerOption(BanTarget target)
    {
        var option = new ButtonMenuOption(target.Name);

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenDurationMenu(args.Player, target));

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private void OpenDurationMenu(IPlayer player, BanTarget target)
    {
        if (!CanBan(player))
        {
            return;
        }

        var builder = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle($"Срок бана: {target.Name}")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();

        AddDurationOption(builder, target, "30 минут", TimeSpan.FromMinutes(30));
        AddDurationOption(builder, target, "1 час", TimeSpan.FromHours(1));
        AddDurationOption(builder, target, "6 часов", TimeSpan.FromHours(6));
        AddDurationOption(builder, target, "1 день", TimeSpan.FromDays(1));
        AddDurationOption(builder, target, "7 дней", TimeSpan.FromDays(7));
        AddDurationOption(builder, target, "30 дней", TimeSpan.FromDays(30));
        AddDurationOption(builder, target, "Навсегда", null);

        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private void AddDurationOption(IMenuBuilderAPI builder, BanTarget target, string title, TimeSpan? duration)
    {
        var option = new ButtonMenuOption(title);

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenReasonModeMenu(args.Player, target, duration));

            return ValueTask.CompletedTask;
        };

        builder.AddOption(option);
    }

    private void OpenReasonModeMenu(IPlayer player, BanTarget target, TimeSpan? duration)
    {
        if (!CanBan(player))
        {
            return;
        }

        var builder = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle($"Причина: {target.Name}")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();

        var predefinedOption = new ButtonMenuOption("Выбрать готовую причину");

        predefinedOption.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenPredefinedReasonMenu(args.Player, target, duration));

            return ValueTask.CompletedTask;
        };

        var customOption = new ButtonMenuOption("Ввести причину вручную");

        customOption.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenCustomReasonMenu(args.Player, target, duration));

            return ValueTask.CompletedTask;
        };

        builder.AddOption(predefinedOption);
        builder.AddOption(customOption);

        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private void OpenPredefinedReasonMenu(IPlayer player, BanTarget target, TimeSpan? duration)
    {
        if (!CanBan(player))
        {
            return;
        }

        var builder = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle($"Причина: {target.Name}")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();

        foreach (var reason in PredefinedReasons)
        {
            var option = new ButtonMenuOption(reason);

            option.Click += (_, args) =>
            {
                ApplyBan(args.Player, target, duration, reason);

                return ValueTask.CompletedTask;
            };

            builder.AddOption(option);
        }

        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private void OpenCustomReasonMenu(IPlayer player, BanTarget target, TimeSpan? duration)
    {
        if (!CanBan(player))
        {
            return;
        }

        var input = new InputMenuOption(
            text: "Введите причину",
            maxLength: 256,
            validator: value => !string.IsNullOrWhiteSpace(value),
            defaultValue: string.Empty,
            hintMessage: "Напишите причину в чат"
        );

        input.ValueChanged += (_, args) => ApplyBan(args.Player, target, duration, args.NewValue);

        var menu = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle($"Причина: {target.Name}")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems()
            .AddOption(input)
            .Build();

        Core.MenusAPI.OpenMenuForPlayer(player, menu);
    }

    private void ApplyBan(IPlayer player, BanTarget target, TimeSpan? duration, string reason)
    {
        if (!CanBan(player) || string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        var administratorSteamId = player.SteamID;

        Core.MenusAPI.CloseActiveMenu(player);

        databaseTasks.Run(
            async () =>
            {
                await banService
                    .BanAsync(target.SteamId, administratorSteamId, duration, reason)
                    .ConfigureAwait(false);

                Core.Scheduler.NextTick(() => EnforceBan(target));
            },
            $"Ban player {target.SteamId}"
        );
    }

    private void EnforceBan(BanTarget target)
    {
        var player = Core.PlayerManager.GetPlayer(target.PlayerId);

        if (player is null || player.IsFakeClient || player.SteamID != target.SteamId)
        {
            return;
        }

        banEnforcementService.Check(player);
    }

    private bool CanBan(IPlayer player)
    {
        return player.IsValid && player.IsAuthorized && privilegeService.HasPermission(player.SteamID, AdminPermissions.Ban);
    }

    private readonly record struct BanTarget(int PlayerId, ulong SteamId, string Name);
}