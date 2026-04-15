using Common.Menu.Data.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;

namespace Common.Menu.Data;

public class MainMenu(ISwiftlyCore core) : BaseMenu(core)
{
    public override IMenuBuilderAPI Builder(IMenuManagerAPI manager, IMenuAPI? parent = null)
    {
        var builder = manager.CreateBuilder()
            .Design
            .SetMenuTitle("Всем дарова ушлепки");

        if (parent is not null)
        {
            builder.BindToParent(parent);
        }

        return builder;
    }
}