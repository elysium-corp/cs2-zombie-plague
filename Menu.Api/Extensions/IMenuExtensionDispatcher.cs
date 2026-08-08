namespace Menu.Api.Extensions;

public interface IMenuExtensionDispatcher
{
    void Dispatch(string menuId, MenuExtensionContext context);
}