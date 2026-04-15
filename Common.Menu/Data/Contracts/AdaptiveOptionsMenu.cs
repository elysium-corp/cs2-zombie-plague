using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;

namespace Common.Menu.Data.Contracts;

public abstract class AdaptiveOptionsMenu(ISwiftlyCore core) : BaseMenu(core)
{
    private readonly List<(int Order, Action<MenuBuildContext> Callback)> _callbacks = [];

    public List<IMenuOption> BuildOptions()
    {
        
    }
    
    public override IMenuBuilderAPI Builder(IMenuManagerAPI manager, IMenuAPI? parent = null)
    {
        var builder = manager.CreateBuilder()
            .Design
            .SetMenuTitle("Всем дарова ушлепки");

        if (parent is not null) builder.BindToParent(parent);
        
        builder.AddOption()

        return builder;
    }
}