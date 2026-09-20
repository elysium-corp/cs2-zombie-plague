# Knife Selector — Panorama v4

`!knife`, `!zknife`, русские алиасы и пункт главного меню открывают Custom HUD.
Строка или SELECT меняют превью. Нижняя кнопка экипирует выделенный нож.
Выбранный нож сохраняется существующим `KnifeService`. Повторная команда,
крестик и Escape закрывают окно. Меню доступно человеку из CT, в том числе до возрождения, как и раньше.
Смерть, заражение, смена команды, карты, отключение и выгрузка освобождают мышь.
Если другой Custom HUD уже захватил ввод, сначала закройте его. При открытии
другого HUD окно ножей уступает ему ввод на ближайшем обновлении.

## Макет и статус проверки

Геометрия перенесена с предоставленного изображения 1703×923 в координаты
Panorama высотой 1080: окно 1376×960, 7 строк, превью, характеристики,
описание и кнопка применения. Используются нативные `Panel`, `Label`, `Button`,
`flow-children`, `fill-parent-flow`, `gradient`, `visibility: collapse`.
`_v4` — ревизия ресурсов Elysium, не версия синтаксиса или компилятора Valve.
Корневая панель не имеет `id`; все адресуемые панели вложены в неё.
У `Label` нельзя задавать атрибут `html`, включая `html="false"`: валидатор
Custom HUD отклоняет весь layout при наличии этого атрибута.

**Пиксельное совпадение пока не подтверждено.** В исходном вложении есть только
общий PNG, без отдельных прозрачных изображений ножей, дымчатой текстуры
превью и шрифта. В комплекте используется штатная иконка ножа CS2 и радиальное
свечение. Фон — текущая игровая сцена под затемнением, без имитации blur через
неподдерживаемый `backdrop-filter`. Результат нужно проверить в игре после
компиляции Workshop Tools. Проверка XML и .NET не является такой компиляцией.

## Установка и нативная компиляция

1. В CS2 Workshop Tools создайте или откройте addon `elysiumhud`.
2. Из корня репозитория запустите:

```powershell
python scripts/generate-knife-hud.py --check
pwsh scripts/compile-knife-hud.ps1 `
  -Cs2Path 'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive' `
  -Addon elysiumhud
```

Скрипт копирует `content/panorama`, компилирует CSS перед XML и проверяет наличие
новых `.vcss_c` / `.vxml_c`. Требуется установленный resourcecompiler из актуальных
CS2 Workshop Tools. Сторонний веб-браузер не заменяет этот этап.

3. В VPK должны попасть:

```text
panorama/layout/custom_game/elysium_knife_selector_v4.vxml_c
panorama/styles/custom_game/elysium_knife_selector_v4.vcss_c
panorama/styles/custom_game/elysium_knife_images_v4.vcss_c
```

А также все пользовательские изображения, добавленные в manifest ниже.
Установите VPK на сервер и подключите его доставку клиентам через Workshop.
Сервер проверяет свои ресурсы, но не может подтвердить установку VPK у клиента.
4. Обновите пакеты `CustomKnife.Core` и `Localization.Core` из сборки проекта.
Обычное меню ножей больше не является запасным интерфейсом. При отсутствии
ресурсов команда сообщает об ошибке, не захватывая курсор.

Если в консоли появляется `Layout contains disallowed attribute html for panel type 'Label'`,
обновите исходники с исправленным генератором и XML, повторите компиляцию выше
и замените `panorama/layout/custom_game/elysium_knife_selector_v4.vxml_c` в VPK.
Доставьте обновлённый VPK серверу и клиентам, затем проверьте открытие меню в игре.
Правка исходного XML или обновление DLL без пересборки и доставки VPK не обновляет
скомпилированный layout, который проверяет CS2.

## Изображения и оформление конкретного ножа

Custom HUD API обновляет строки и классы, но не устанавливает `Image.src`.
Поэтому изображения выбираются из скомпилированного списка CSS-классов.
HTTP-поле `image_url` из Flute не превращается автоматически в ресурс VPK.

1. Положите прозрачный PNG в
`content/panorama/images/custom_game/elysium/knives/karambit.png`.
2. Добавьте запись в `knife-hud-assets.json`:

```json
{
  "knife": "s2r://panorama/images/icons/equipment/knife.vsvg",
  "karambit": "s2r://panorama/images/custom_game/elysium/knives/karambit_png.vtex"
}
```

3. Выполните `python scripts/generate-knife-hud.py`, пересоберите плагин и ресурсы.
PNG должен быть скомпилирован Workshop Tools в `karambit_png.vtex_c` и включён
в VPK. Имя `karambit` в конфиге и manifest должно совпадать. Неизвестное имя
безопасно использует стандартную иконку `knife`.
4. Настройте `configs/plugins/CustomKnife.Core/knife-hud.json`:

```json
{
  "KnifeHud": {
    "RefreshIntervalSeconds": 0.25,
    "IdleTimeoutSeconds": 120,
    "Knives": {
      "knife_karambit": {
        "Image": "karambit",
        "Rarity": "Legendary",
        "SubtitleKey": "CustomKnife.Karambit.Subtitle",
        "BenefitKeys": [
          "CustomKnife.Karambit.Benefit.One",
          "CustomKnife.Karambit.Benefit.Two"
        ]
      }
    }
  }
}
```

`knife_karambit` — пример: используйте реальный `internal_name` из каталога.
Конфиг оформления не создаёт ножи и не изменяет характеристики или права.
Поддерживаются Common, Uncommon, Rare, Restricted, Classified, Elite, Prototype,
Legendary. Без `BenefitKeys` выводятся реальные скорость, множитель урона,
гравитация и отдача; без `SubtitleKey` — описание. Имя и описание используют
существующие ключи ножей; 27 новых ключей `Menu.Knife.Hud.*` доступны в
Localization.Core для ru/en/de/pl. В английском языке подписи соответствуют макету.

## Проверка

```bash
python3 scripts/generate-knife-hud.py --check
python3 -m unittest discover -s scripts/tests -p 'test_knife_hud_resources.py' -v
dotnet test CustomKnife.Core.Tests/CustomKnife.Core.Tests.csproj -c Release
```

В игре проверьте две одновременные сессии, 1/7/8+ ножей, длинные переводы,
отзыв прав при открытом окне, reload каталога, быстрые клики по страницам,
закрытие по Escape/смерти/заражению и освобождение мыши при выгрузке.
Визуальное сравнение выполняйте при одинаковом разрешении и HUD scale.

Нет `OnTick`, SQL-запросов или JavaScript в интерфейсе. Таймер работает только
при открытых окнах и читает memory snapshot. Сетевые обновления отправляются
только при изменении строк/классов. Каждая сессия проверяет владельца и
`SessionId`; смена страницы/каталога создаёт новую сущность, отклоняя старые клики.
Кнопки подтверждения привязаны к конкретному видимому слоту, а доступ проверяется
ещё раз перед экипировкой.

API: https://swiftlys2.net/docs/development/custom-hud
