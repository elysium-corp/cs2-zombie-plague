# Localization.Core

Единая серверная локализация Elysium.

Язык игрока определяется в строгом порядке:

1. ручной выбор из `localization.player_preferences`;
2. язык клиента CS2;
3. `server_fallback_language`.

`ILocalizationApi.GetForPlayer` читает только immutable memory snapshot. PostgreSQL
используется при подключении, смене языка и периодическом обновлении snapshot.
При сбое БД сохраняется last-known-good cache или используется проверенный
`resources/templates/template.jsonc`.

Игрок открывает меню языков командами `!lang`, `!language` и `!язык`.
Администраторские команды: `localization_status`, `localization_reload`.
