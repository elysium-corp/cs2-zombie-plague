using Menu.Api.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Data.Contracts;

public abstract class DynamicOptionsMenu(ISwiftlyCore core, IEventPublisher eventPublisher) : BaseMenu(core)
{
    protected virtual Action<IPlayer, MenuOptionsHolder>? MenuBuilderCallback => null;

    private IMenuBuilderAPI DynamicOptionsBuilder(IPlayer player, IMenuBuilderAPI baseBuilder)
    {
        var menuType = GetType();
        var optionsHolder = new MenuOptionsHolder();
        
        eventPublisher.OnMenuAddOption(player, menuType, optionsHolder);
        
        MenuBuilderCallback?.Invoke(player, optionsHolder);

        var options = optionsHolder
            .BuildOptions()
            .Select(option => option.Option);

        foreach (var option in options)
        {
            baseBuilder.AddOption(option);
        }

        return baseBuilder;
    }

    public override IMenuBuilderAPI Builder(IPlayer player)
    {
        var baseBuilder = BaseBuilder(player);

        var builder = DynamicOptionsBuilder(player, baseBuilder);

        return builder;
    } 

    public record IMenuOptionWrapper(IMenuOption Option, int? Priority);

    public sealed class MenuOptionsHolder
    {
        private readonly List<IMenuOptionWrapper> _options = [];

        public void Add(IMenuOption option, int priority = int.MaxValue)
        {
            var optionWrapper = new IMenuOptionWrapper(option, priority);
            _options.Add(optionWrapper);
        }

        public List<IMenuOptionWrapper> BuildOptions()
        {
            return _options.OrderBy(option => option.Priority).ToList();
        }
    }
}