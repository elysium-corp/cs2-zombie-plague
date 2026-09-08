> Обновление ресурсов v4 r2 обязательно для этой версии: [установка, стопки, роли и анимации](../docs/banner-enhancements.md)

# Elysium Custom HUD

`CustomHud.Core 1.2.0` предоставляет `CustomHud.Api.ICustomHudApi` другим плагинам
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
Обновите вместе с ним `ZombiePlague.Core 0.5.0`, `Advertisement.Core 2.8.0`, `Localization.Core 1.5.5`
Существующий HUD способностей v4 остаётся отдельным ресурсом
Ресурсы сообщений теперь тоже имеют суффикс `_v4`, чтобы не смешивать их с прежними несовместимыми вариантами HUD
Суффикс означает редакцию ресурса, а не версию API Panorama; простое переименование старого скомпилированного файла не заменяет пересборку
Оба layout используют ограниченный набор атрибутов Custom HUD, проверяемый тестами

В 1.1.1 удалены процентные max-width, сжимавшие панель баннера, и задана минимальная ширина в единицах Panorama. Эта часть исправления находится в CSS внутри VPK: замены DLL недостаточно. Список sw plugins list показывает версии серверных плагинов и не подтверждает актуальность ресурсов у клиента

1. Скопируйте содержимое `resources/hud/messages/content/panorama/` в `content/csgo_addons/<addon>/panorama/` на машине с CS2 Workshop Tools
2. Скомпилируйте `layout/custom_game/elysium_messages_v4_r2.xml` и `styles/custom_game/elysium_messages_v4_r2.css`
3. Упакуйте SVG из `images/custom_game/elysium/banners/` вместе с layout и styles. В VPK должны попасть `panorama/layout/custom_game/elysium_messages_v4_r2.vxml_c` и `panorama/styles/custom_game/elysium_messages_v4_r2.vcss_c`
4. Доставьте обновлённый VPK серверу и клиентам, затем смените карту
5. Выполните `custom_hud status`, затем `custom_hud test TopCenter`

`custom_hud test` из консоли сервера отправляет пример всем, из игры — только отправителю
Права команды: `custom_hud.admin`; доступны `on`, `off`, `status`, `test [Position]`
`custom_hud.json`, секция `CustomHud`, содержит `Enabled: true`
Команда `on` повторяет создание сущности, `off` очищает сообщения и отключает её до следующего `on` или перезапуска

Компилятор Workshop Tools и игровой клиент не входят в .NET-сборку
Перед установкой на сервер проверьте баннер, разноцветный текст, меню, все используемые позиции и смену карты в CS2

## Игровые уведомления

Начало раунда и остальные всплывающие уведомления плагинов используют `IBannerNotificationApi` из Advertisement.Core 3.0.0. Настройка находится в **Реклама → Баннеры → Плагины**. Отдельный `round_hud.json` больше не используется. Начало режима — событие `Game.Round.Started`, текст по умолчанию `Notifications.Game.Round.Started` с `{round}` и `{roundName}`.

[Настройки событий, полный каталог параметров и порядок установки](../docs/banner-notifications.md).

## Реклама

Существующие аудитории, расписания, параметры и переводы Advertisement сохраняются
Выбор способа доставки хранится в `Advertisement.Core/hud_delivery.json`, секция `AdvertisementHud`
Настройки отдельного сообщения в Flute CMS имеют приоритет над локальной конфигурацией доставки

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
Явный режим HUD-only не отправляет сообщения в чат при недоступности HUD. Старый локальный fallback без DisplayType сохраняет прежнее поведение
Реклама использует приоритет 0, баннеры раунда — 100
После изменения настроек доставки перезагрузите Advertisement.Core
Сами объявления по-прежнему редактируются через существующую админку

## Источники ограничений движка

- [SwiftlyS2 Custom HUD](https://swiftlys2.net/docs/development/custom-hud)
- [Список разрешённых элементов и атрибутов layout](https://github.com/Kxnrl/vsc-panorama-ext/blob/b4b7afe9ccd01d9787ed583ec8b83a925faab2fe/src/core/mode.ts)
- [Лимиты идентификаторов и устройство Custom HUD](https://github.com/Wend4r/s2r-skills/blob/82fd9c366dec51edf19b801a102dce86fd695950/custom-hud-layout/references/internals.md)

Настройки доставки рекламы из Flute описаны в [Advertisement.Core](../Advertisement.Core/README.md#настройка-через-flute-cms)


## Конструктор баннеров и дополнительный API

`CustomHud.Core 1.2.0` экспортирует `ICustomBannerApi` по ключу `CustomHud.Api.ICustomBannerApi`
Существующий `ICustomHudApi` продолжает работать. Каналы, девять позиций, приоритеты и `Hide`/`ClearChannel` общие для обоих API

```csharp
interfaces.TryGetSharedInterface<ICustomBannerApi>(ICustomBannerApi.SharedApiKey, out var banners);
var template = new HudBannerTemplate
{
    Variant = "feature", Icon = "infection", Theme = "danger", Accent = "red",
    Enter = "zoom", Exit = "fade", IconAnimation = "pulse",
    Sound = "ZombiePlagueSounds.round_start_2", Volume = 0.5f
};
banners?.ShowLocalized(player, template, new HudBannerContent
{
    Header = "Banner.Round.Header", Title = "Banner.Round.Title", Description = "Banner.Round.Description"
}, new Dictionary<string, object?>
{
    ["player_name"] = player.Name, ["round"] = 7, ["mode"] = "Массовое заражение"
}, new HudMessageOptions
{
    Channel = "MyPlugin.Round", Position = HudPosition.TopCenter, DurationSeconds = 6, Priority = 100
});
```

Сначала создайте ключи примера в общей Localization: Header = `РАУНД {round}`, Title = `{mode}`, Description = `Приготовься, {player_name}!`
Передавайте исходные значения параметров: API сам экранирует строки до подстановки; число/boolean сохраняет тип
Переводы выбираются для игрока с обычным fallback Localization; отсутствие обязательного перевода возвращает false
Для готовых текстов используйте `Show(player, template, content, options)` и самостоятельно экранируйте вставляемые имена через `HudText.Escape`
Повторный `Show` создаёт новый экземпляр. Обновления сетевого состояния существующего экземпляра не повторяют анимацию и звук

| Variant | Обязательные поля | Иконка |
| --- | --- | --- |
| text | Description | отсутствует |
| icon | Description | обязательна |
| headline | Title, Description | необязательна |
| feature | Header, Title, Description | обязательна |
| hero | Header, Title, Description | необязательна, удобно размещать сверху |
| custom | Только блоки с ShowHeader/ShowTitle/ShowDescription = true, хотя бы один | необязательна |

Header/Title ограничены одной строкой каждый, Description — четырьмя, до 12 цветных фрагментов в строке и 4096 UTF-16 символов на поле
Ширина панели: small — 440, medium — 600, large — 760 единиц Panorama. Для переноса учитываются отступы, место иконки, размер текста и отдельные шрифты Header/Title/Description. Переполнение помечается многоточием
Дизайн поддерживает 6 фонов, 3 ширины, 3 размера текста, выравнивание, углы, границы и произвольный HEX-акцент, округляемый до палитры игры
Enter/Exit: none, fade, slide_up, slide_down, slide_left, slide_right, zoom. Speed: fast — 0,2 с, normal — 0,4 с, slow — 0,8 с
На выходе сообщение сохраняется до конца эффекта после TTL. Новое сообщение сразу заменяет выходящее; отключение игрока и карты очищает всё немедленно
Иконка: info, warning, infection, skull, shield, trophy, star, gift, megaphone, lightning, clock, heart; эффекты none/pulse/spin/bounce/shake
Новый SVG требует изменения каталога, пересборки layout и доставки VPK. Произвольные URL, скрипты и CSS не отправляются клиенту

Sound — имя установленного sound event, а не путь, URL или консольная команда. Volume: 0–1, пустой Sound отключает звук
Он адресуется получателю при первом фактическом показе. Перекрытый баннер молчит до показа; возобновление и восстановление сущности не повторяют звук
Браузерный Preview может проиграть отдельно указанную HTTPS-аудиоверсию; соответствие этой записи игровому sound event проверяется администратором

Ресурс остаётся `elysium_messages_v4`, но его содержимое расширено. Обязательно пересоберите и доставьте новый VPK вместе с плагином
`SchemaVersion = 1` обозначает первую схему JSON-дизайна и не имеет отношения к редакции клиентского layout v4
Исходник генератора ресурсов: `scripts/generate-banner-resources.py`. Макет содержит 238 уникальных panel IDs на область, меньше лимита 1024

Точные настройки доступны и через API: WidthPixels 320–960 (шаг 40), Padding 0–40 (4), Gap 0–24 (2), HeaderSize 10–24 (2), TitleSize 16–48 (2), DescriptionSize 12–32 (2), IconSize 24–96 (8). `null` использует размер темы. Background выбирает theme/slate/black/blue/purple/red/green/gold/white; BackgroundOpacity 0–100 (10) меняет только фон. HeaderColor/TitleColor/DescriptionColor и Shadow управляют цветом блока и тенью. Перечень цветов и других значений находится в XML-документации HudBannerTemplate.
