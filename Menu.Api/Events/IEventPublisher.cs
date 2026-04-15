using Menu.Api.Data.Contracts;

namespace Menu.Api.Events;

public interface IEventPublisher
{
    void OnMenuAddOption(Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder);
}