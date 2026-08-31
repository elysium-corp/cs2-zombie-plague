# Localization.Core

Единая серверная локализация Elysium.

Язык игрока определяется в строгом порядке:

1. ручной выбор из `localization.player_preferences`;
2. язык клиента CS2;
3. `server_fallback_language`.

`ILocalizationApi.GetForPlayer` читает только immutable memory snapshot. PostgreSQL
используется при подключении, смене языка, старте карты и ручном обновлении snapshot.
При сбое БД сохраняется last-known-good cache или используется проверенный
`localization.json` из каталога конфигурации плагина.

Проверенный fallback-файл должен находиться здесь:

```text
(swRoot)/configs/plugins/Localization.Core/localization.json
```

Скачайте `localization.json` в модуле ElysiumLocalization во Flute CMS и поместите
его по этому пути. Файл читается как корневой JSON, без дополнительной секции.
Если файл отсутствует или остался пустой шаблон старой версии с ключом `""`,
плагин использует встроенный аварийный snapshot. Любой другой невалидный конфиг
не подменяется аварийным snapshot молча, а отклоняется с ошибкой валидации.

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
и запишет rate-limited предупреждение. Старые методы со словарём строк сохранены.
