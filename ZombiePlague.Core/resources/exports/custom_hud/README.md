# Zombie class Custom HUD

Этот каталог содержит исходники клиентской Panorama-разметки для карточки класса зомби.
Плагин создаёт `custom_hud_layout` динамически, но клиентские XML, CSS и изображения должны быть
скомпилированы Resource Compiler и доставлены игрокам через Workshop VPK.

Путь ресурса после сборки должен остаться неизменным:

```text
panorama/layout/custom_game/elysium/zombie_class_card.xml
```

Для импорта в VPK Manager используйте содержимое каталога `content` как исходный addon content.
Изображения в `content/panorama/images/custom_game/elysium/zombie_classes` являются заменяемыми
векторными заглушками: имена и пути файлов можно сохранить, подменив графику без изменения C#.
