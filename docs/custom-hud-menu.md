# Custom HUD Menu API

Начиная со SwiftlyS2 `v1.4.6-beta.9`, `Menu.Core` поддерживает интерактивные
Panorama-меню через сущность `custom_hud_layout`.

API решает общие задачи за плагины-потребители:

- создаёт одну HUD-сущность на зарегистрированный layout;
- маршрутизирует `CCSUsrMsg_CustomHudClicked` только в активную сессию игрока;
- применяет персональные dialog variables и CSS-классы;
- разрешает только одно активное HUD-меню на игрока;
- освобождает input capture при закрытии, disconnect, смене карты и reload;
- удаляет сущность после освобождения регистрации.

## Регистрация

Плагин получает `IMenuApi` как shared-интерфейс и один раз регистрирует layout:

```csharp
private IDisposable? _registration;

private void Initialize(IMenuApi menuApi)
{
    _registration = menuApi.Hud.Register(
        new HudMenuDefinition(
                id: "example.card",
                layoutPath: "panorama/layout/custom_game/example/card.xml",
                rootPanelId: "ExampleCard")
            .AddButton("ExampleSelect", context =>
            {
                // Валидация и применение выбора выполняются на сервере.
                context.Menu.Close(context.Player);
            })
            .AddButton("ExampleClose", context => context.Menu.Close(context.Player))
    );
}

private void Uninitialize()
{
    _registration?.Dispose();
    _registration = null;
}
```

Регистрацию нужно освободить в `OnUnload`, пока shared-зависимости ещё доступны.

## Открытие

```csharp
var view = new HudMenuView()
    .WithState(domainObject)
    .SetDialogVariable("ExampleTitle", "value", "Карточка")
    .SetDialogVariable("ExampleDescription", "value", "Описание")
    .SetClass("ExampleImage", "is-active", true);

menuApi.Hud.Open(player, "example.card", view);
```

`WithState` передаёт серверный объект в `HudMenuButtonContext.State`. Клиент не
может подменить это состояние: из пользовательского сообщения принимается только
идентификатор кнопки, после чего он сверяется с активным меню, entity handle и
зарегистрированным обработчиком.

## Клиентские ресурсы

`custom_hud_layout` не отправляет XML, CSS и изображения с игрового сервера.
Их необходимо скомпилировать Resource Compiler, упаковать в Workshop VPK и
подключить игрокам. Исходники карточки классов находятся в:

```text
ZombiePlague.Core/resources/exports/custom_hud/content
```

После сборки layout должен быть доступен по пути, который передан в
`HudMenuDefinition.LayoutPath`.
