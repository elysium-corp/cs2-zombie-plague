using Menu.Api.Data.Contracts;
using Menu.Api.Events;

namespace Menu.Api;

public interface IMenuApi
{
    public IEventSubscriber EventSubscriber { get; }

    public TMenu CreateMenu<TMenu>() where TMenu : class, IMenu;
    
    public static readonly string SharedApiKey = "Menu.Api.IMenuApi";
}