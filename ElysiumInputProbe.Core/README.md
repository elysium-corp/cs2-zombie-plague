# ElysiumInputProbe

Диагностический SwiftlyS2-плагин для проверки цифрового input через `CBaseUserCmdPB.weaponselect`.

## Версия 0.5.0

По предыдущим тестам подтверждено:

- `1`, `2`, `3` стабильно формируют `weaponselect`, включая повторное нажатие уже активного слота;
- `6`, `7`, `8` стабильно распознаются через HE / Flash / Smoke;
- зануление `Weaponselect` в `ProcessUsercmds.Pre` подавляет фактическое переключение предмета;
- `HIDEHUD_WEAPONSELECTION` не ломает capture-механику.

Версия `0.4.0` показала, что `GiveItem` может вернуть валидную entity, но игра не обязана добавить её в `MyValidWeapons`. Поэтому в `0.5.0`:

1. Добавлен изолированный тест одной клавиши: `capture key 5|6|7|8|9|0`.
2. Лог различает `existing-attached`, `created-attached` и `created-not-attached`.
3. Все созданные probe entity отслеживаются по index.
4. При `capture off`, `inputprobe off`, disconnect или unload предмет из инвентаря удаляется через `RemoveWeapon`, а неприкреплённая entity принудительно `Despawn()`.
5. Weapon-selection HUD скрывается только на время capture и возвращается после него.
6. Для распознанного `slot5..slot10` `Weaponselect` зануляется на каждой копии usercmd, а лог пишется только один раз на `CommandNumber`.

## Команды

```text
!inputprobe on
!inputprobe status
!inputprobe capture key 5
!inputprobe capture key 9
!inputprobe capture key 0
!inputprobe capture all
!inputprobe capture off
!inputprobe off
```

## Рекомендуемый тест 5 / 9 / 0

Тестировать по одному, чтобы не упираться в ограничения grenade inventory.

### C4 / клавиша 5

```text
!inputprobe on
!inputprobe capture key 5
```

Нажать несколько раз `5`.

Если C4 реально прикрепился к инвентарю, ожидается:

```text
[InputProbe][INJECT] ... key=5 ... status=created-attached
[InputProbe][CAPTURE] ... key=5 slot=slot5 weapon=weapon_c4 ... suppressed=True
```

Если игра создала C4 entity, но не дала её игроку:

```text
[InputProbe][INJECT] ... key=5 ... status=created-not-attached
```

После теста:

```text
!inputprobe capture off
```

Для orphan entity ожидается:

```text
[InputProbe][CLEANUP] ... status=despawned-orphan
```

### Decoy / клавиша 9

```text
!inputprobe capture key 9
```

Нажать `9` несколько раз.

### Molotov / клавиша 0

```text
!inputprobe capture key 0
```

Нажать `0` несколько раз. Для CT клиент может разрешить `slot10` в `weapon_incgrenade`; probe это тоже распознаёт.
