using CustomEquipment.Api.Enums;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace CustomEquipment.Menus;

internal sealed class EquipmentMenu(ISwiftlyCore core) : MenuBase(core)
{
    public override string Id => "equipment.menu.select-equipment";
    
    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.Players;
    
    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "equipment",
        "weapons",
        "оружия",
        "пушки",
        "цуфзщты",
        "уйгшзьуте"
    ];
    
    private const string EquipmentMenuTitle = "Menu.Equipment.Title";

    private const string BaseCategoryMenuPath = "Menu.Equipment.Category";
    
    private static readonly Category[] Categories = Enum
        .GetValues<WeaponType>()
        .Select(WeaponTypeToCategory)
        .ToArray();

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        BuildCategories(builder, localizer);
        
        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        return design.SetMenuTitle(localizer[EquipmentMenuTitle])
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private void BuildCategories(IMenuBuilderAPI builder, ILocalizer localizer)
    {
        var categories = Categories.OrderBy(category => category.Order);
        
        foreach (var category in categories)
        {
            BuildCategory(builder, localizer, category);
        }
    }

    private void BuildCategory(IMenuBuilderAPI builder, ILocalizer localizer, Category category)
    {
        var option = new ButtonMenuOption
        {
            Text = localizer[category.NameLocalizationKey]
        };

        builder.AddOption(option);
    }

    private static Category WeaponTypeToCategory(WeaponType weaponType, int index)
    {
        return new Category(
            NameLocalizationKey: $"{BaseCategoryMenuPath}.{weaponType}",
            Order: index
        );
    }

    private sealed record Category(string NameLocalizationKey, int Order);
}