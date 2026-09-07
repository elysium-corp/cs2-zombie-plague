# Elysium Custom HUD

`CustomHud.Core 1.0.0` предоставляет `CustomHud.Api.ICustomHudApi` другим плагинам
HUD сообщений использует собственную сущность и сосуществует с меню SwiftlyS2 и панелью способностей

## Вызов из плагина

Добавьте ссылку на проект `CustomHud.Api` и получите общий интерфейс после регистрации API плагинов
Для `Common.Di.Plugin<T>` это `OnSharedInterfacesInjected`, для обычного `BasePlugin` — `OnSharedInterfaceInjected`
Повторное внедрение интерфейсов должно обновлять сохранённую ссылку

```csharp
using CustomHud.Api;

// В обработчике внедрения shared-интерфейсов
interfaces.TryGetSharedInterface<ICustomHudApi>(ICustomHudApi.SharedApiKey, out _hud);

// В игровом событии — отправка одинакового текста всем текущим игрокам
_hud?.Broadcast("<font color='mint'><b>Массовое заражение</b></font>",
    new HudMessageOptions
    {
        Channel = "MyPlugin.Round",
        Position = HudPosition.TopCenter,
        Style = HudMessageStyle.Banner,
        DurationSeconds = 6,
        Priority = 100
    });

// Личное уведомление
_hud?.Show(player, "<font color='gold'>Скидка</font> в магазине Elysium",
    new HudMessageOptions { Channel = "MyPlugin.Ad", Position = HudPosition.BottomRight });

// При выгрузке владельца удалите его сообщения
_hud?.ClearChannel("MyPlugin.Round");
_hud?.ClearChannel("MyPlugin.Ad");
```

Простой вызов `_hud?.Show(player, "Сообщение")` показывает текст на 6 секунд сверху по центру
В интеграциях нескольких плагинов всегда задавайте уникальный `Channel`
`Hide(player, channel)` убирает канал у одного игрока, `ClearChannel(channel)` — у всех
Очистка и показ применяются на ближайшем обновлении HUD с периодом 0,1 секунды

Все вызовы API выполняются на игровом потоке, включая чтение `IsAvailable`
Из фонового кода используйте `Core.Scheduler.NextWorldUpdate`, заново найдите игрока по слоту и проверьте SteamID
Не сохраняйте `IPlayer` для отложенной отправки после его отключения
Возвращаемое `true` означает приём сообщения сервером, а не подтверждение отрисовки клиентом
Если провайдер или ресурсы недоступны, `Show` возвращает `false`, `Broadcast` — `0`

## Разметка

Поддерживаются `<font color='red'>`, `<font color='#85dcb1'>`, `<span style='color: #85dcb1'>`,
`<b>`, `<strong>`, `<i>`, `<em>`, `<u>`, `<br>`, HTML-сущности и цветовые теги Localization `[red]…[/]`
Закрывающий `[/]` сбрасывает оформление, вложенные HTML-теги восстанавливают предыдущее оформление
Неизвестные HTML-теги удаляются с сохранением текста внутри
Ссылки, картинки, скрипты, произвольный CSS и HTML-атрибуты не выполняются

Разметка разбирается на сервере в отдельные Label и CSS-классы
Атрибут `html` не используется: он отсутствует в разрешённом наборе Custom HUD
Цвета выбираются из 235 заранее скомпилированных значений, включая точные цвета Elysium
Произвольный `#RGB` / `#RRGGBB` приводится к ближайшему цвету этой палитры
Именованные цвета: white, black, red, darkred, green, lightgreen, lime, blue, lightblue, cyan,
yellow, gold, orange, purple, lightpurple, pink, gray/grey, silver, mint, muted, default, olive, lightyellow, bluegrey, darkblue, magenta, lightred

Для ника или другого внешнего значения используйте `HudText.Escape(value)` **до** подстановки в шаблон
Для полностью буквального вывода задайте `Format = HudTextFormat.PlainText`
Вход ограничен 4096 символами, превышение и неверные options дают `ArgumentException`
Отрисовываются до четырёх строк по 52 символа, для баннера — по 40; длинный текст заканчивается многоточием
До 12 цветных фрагментов на строку; при превышении оставшийся текст сохраняется с оформлением последнего фрагмента
Переносы выполняются по словам, символы вне BMP не разрезаются

## Области и конкуренция

Позиции: TopLeft, TopCenter, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, BottomCenter, BottomRight
`TopCenter` находится ниже верхней полосы игроков; отступ задан в CSS относительно безопасной зоны Panorama
Точное положение проверьте в игре на используемом масштабе интерфейса

В каждой области показывается одно сообщение, в разных областях сообщения могут отображаться одновременно
В одной области больший `Priority` перекрывает меньший, при равенстве показывается более новое сообщение
Повторный вызов с теми же каналом и позицией заменяет текст и обновляет время показа
Перекрытое сообщение может вернуться только до истечения первоначального срока жизни
Срок показа — от 0,5 до 60 секунд, приоритет — от 0 до 1000
На игрока хранится не более 32 сообщений, включая перекрытые

Смена карты, отключение игрока и выгрузка очищают сообщения
При удалении сущности сервис восстанавливает её и оставшиеся сообщения, максимум трижды за карту
Если ресурсов нет, повторная попытка выполняется на следующей карте или через `custom_hud on`
Меню не скрывают сообщения; HUD не захватывает мышь и клавиатуру

## Установка и проверка

Установите `CustomHud.Core` из общего runtime-пакета, сохранив его `resources/exports/CustomHud.Api.dll`
Обновите вместе с ним `ZombiePlague.Core 0.5.0`, `Advertisement.Core 2.6.0`, `Localization.Core 1.5.5`
Существующий HUD способностей v4 остаётся отдельным ресурсом

1. Скопируйте содержимое `resources/hud/messages/content/panorama/` в `content/csgo_addons/<addon>/panorama/` на машине с CS2 Workshop Tools
2. Скомпилируйте `layout/custom_game/elysium_messages_v1.xml` и `styles/custom_game/elysium_messages_v1.css`
3. В VPK должны попасть `panorama/layout/custom_game/elysium_messages_v1.vxml_c` и `panorama/styles/custom_game/elysium_messages_v1.vcss_c`
4. Доставьте обновлённый VPK серверу и клиентам, затем смените карту
5. Выполните `custom_hud status`, затем `custom_hud test TopCenter`

`custom_hud test` из консоли сервера отправляет пример всем, из игры — только отправителю
Права команды: `custom_hud.admin`; доступны `on`, `off`, `status`, `test [Position]`
`custom_hud.json`, секция `CustomHud`, содержит `Enabled: true`
Команда `on` повторяет создание сущности, `off` очищает сообщения и отключает её до следующего `on` или перезапуска

Компилятор Workshop Tools и игровой клиент не входят в .NET-сборку
Перед установкой на сервер проверьте баннер, разноцветный текст, меню, все используемые позиции и смену карты в CS2

## Начало раунда

`ZombiePlague.Core` показывает баннер после успешного запуска режима, через событие `Rounds.Started`
Поддержаны Инфекция, Массовое заражение, Немезида и Выживший; неизвестный режим использует своё имя
Подготовка и отклонённая попытка запуска баннера не создают, окончание режима удаляет его
Текст локализуется для каждого игрока ключом `ZombiePlague.Round.Hud.Started`, параметр `{mode}`
Параметры в `ZombiePlague.Core/round_hud.json`:

```json
{
  "RoundHud": {
    "Enabled": true,
    "DurationSeconds": 6,
    "Position": "TopCenter"
  }
}
```

## Реклама

Существующие аудитории, расписания, параметры и переводы Advertisement сохраняются
Выбор способа доставки хранится в `Advertisement.Core/hud_delivery.json`, секция `AdvertisementHud`
Это серверная настройка; отдельные поля редактора Flute CMS в данный PR не входят

```json
{
  "AdvertisementHud": {
    "Mode": "Hud",
    "Position": "BottomLeft",
    "DurationSeconds": 8,
    "Messages": {
      "Discord": { "Mode": "ChatAndHud", "Position": "TopRight", "DurationSeconds": 10 },
      "Rules": { "Mode": "Hud", "Position": "BottomRight", "DurationSeconds": 8 }
    }
  }
}
```

`Messages` индексируется ключом объявления, а не ключом Localization
Режимы: `Chat`, `Hud`, `ChatAndHud`; по умолчанию сохраняется `Chat`
Без доступного Custom HUD или при отказе принять сообщение остаётся вывод в чат
Реклама использует приоритет 0, баннеры раунда — 100
После изменения настроек доставки перезагрузите Advertisement.Core
Сами объявления по-прежнему редактируются через существующую админку

## Источники ограничений движка

- [SwiftlyS2 Custom HUD](https://swiftlys2.net/docs/development/custom-hud)
- [Список разрешённых элементов и атрибутов layout](https://github.com/Kxnrl/vsc-panorama-ext/blob/b4b7afe9ccd01d9787ed583ec8b83a925faab2fe/src/core/mode.ts)
- [Лимиты идентификаторов и устройство Custom HUD](https://github.com/Wend4r/s2r-skills/blob/82fd9c366dec51edf19b801a102dce86fd695950/custom-hud-layout/references/internals.md)
