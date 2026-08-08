namespace Menu.Api.Extensions;

public interface IMenuExtensionRegistry
{
    IDisposable Subscribe(string menuId, MenuExtensionHandler handler);
}