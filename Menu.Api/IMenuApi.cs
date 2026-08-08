using Menu.Api.Extensions;

namespace Menu.Api;

public interface IMenuApi
{
    IMenuExtensionDispatcher Dispatcher { get; }

    IMenuExtensionRegistry Extensions { get; }
    
    static readonly string SharedApiKey = "Menu.Api.IMenuApi";
}