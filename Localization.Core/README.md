# Localization.Core

Версия `1.3.0` использует только два источника данных: PostgreSQL и проверенный
fallback-файл. Переводы внутри runtime-классов отсутствуют.

Единая серверная локализация Elysium.

Язык игрока определяется в строгом порядке:

1. ручной выбор из `localization.player_preferences`;
2. язык клиента CS2;
3. `server_fallback_language`.

`ILocalizationApi.GetForPlayer` читает только immutable memory snapshot. При запуске
и в конце каждой карты координатор сначала запрашивает PostgreSQL, а при ошибке —
проверенный `localization.json`. Успешно полученный snapshot атомарно записывается
в memory cache. Если оба источника недоступны, текущий snapshot остаётся без изменений.

Проверенный fallback-файл должен находиться здесь:

```text
(swRoot)/configs/plugins/Localization.Core/localization.json
```

Скачайте `localization.json` в модуле ElysiumLocalization во Flute CMS и поместите
его по этому пути. Файл читается как корневой JSON, без дополнительной секции.
Если файл отсутствует, пуст или не проходит валидацию/checksum, он отклоняется.
Встроенного аварийного набора строк нет.

Игрок открывает меню языков командами `!lang`, `!language` и `!язык`.
Администраторские команды: `localization_status`, `localization_reload`.

Типизированные параметры задаются для ключа в ElysiumLocalization и передаются
через `FormatForPlayer` или `FormatForLanguage`:

```csharp
var text = localization.FormatForPlayer(
    player,
    "Profile.Nickname",
    new Dictionary<string, object?> { ["nickname"] = "fdrinv" });
```

Для шаблона `Ваш ник: {nickname}` метод вернёт `Ваш ник: fdrinv`. Если обязательный
параметр отсутствует или его значение не соответствует типу, метод вернёт `null`
и запишет rate-limited предупреждение. Цветовые коды из динамических значений
удаляются, поэтому ник игрока не может внедрить разметку. Старые методы со
словарём строк сохранены.

Цветовые теги настраиваются централизованно в ElysiumLocalization. Встроенные
алиасы `{accent}`, `{warning}`, `{success}`, `{important}` и `{muted}` можно
переназначить, а дополнительные теги — создать в настройках. `Localization.Core`
преобразует их в цветовые коды Swiftly перед возвратом текста, поэтому одна и та
же разметка работает во всех подключённых модулях. Fallback schema v3 хранит
словарь `colorTags`; схемы v1 и v2 остаются совместимыми. Любой встроенный цвет
Swiftly доступен через парный тег `{color:green}...{/color}`.

Периодического polling нет. Устаревшее поле `refresh_interval_seconds` остаётся
в БД и fallback только для совместимости; snapshot обновляется при запуске плагина,
в конце карты или вручную командой `localization_reload`.
