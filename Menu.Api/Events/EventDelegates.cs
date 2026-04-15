using Menu.Api.Data.Contracts;

namespace Menu.Api.Events;

public static class EventDelegates
{
    public delegate void OnMenuAddOption(Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder);
}