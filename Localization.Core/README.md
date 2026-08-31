# Localization.Core

Единая серверная локализация Elysium.

Язык игрока определяется в строгом порядке:

1. ручной выбор из `localization.player_preferences`;
2. язык клиента CS2;
3. `server_fallback_language`.

`ILocalizationApi.GetForPlayer` читает только immutable memory snapshot. PostgreSQL
используется при подключении, смене языка и периодическом обновлении snapshot.
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
