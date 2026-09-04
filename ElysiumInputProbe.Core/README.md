# ElysiumInputProbe

Диагностический SwiftlyS2-плагин для проверки цифрового input через `CBaseUserCmdPB.weaponselect`.

## Версия 0.4.0

Текущий эксперимент проверяет `5, 6, 7, 8, 9, 0`:

- `5` → `slot5` → `weapon_c4`;
- `6` → `slot6` → `weapon_hegrenade`;
- `7` → `slot7` → `weapon_flashbang`;
- `8` → `slot8` → `weapon_smokegrenade`;
- `9` → `slot9` → `weapon_decoy`;
- `0` → `slot10` → `weapon_molotov` / `weapon_incgrenade`.

При включённом capture плагин:

1. Ставит только бит `HIDEHUD_WEAPONSELECTION` в `CBasePlayerPawn.HideHUD`, не трогая остальные HUD-флаги.
2. Выдаёт только отсутствующие тестовые предметы, включая C4.
3. Запоминает entity index только предметов, созданных самим probe.
4. Ловит `weaponselect` в `ProcessUsercmds.Pre`.
5. Для тестовых slot-команд зануляет `Weaponselect`, чтобы предмет не переключался в руках.
6. При выключении capture удаляет только созданные probe предметы и снимает HUD-бит только если probe сам его установил.
7. На unload выполняет ту же очистку.

## Команды

```text
!inputprobe on
!inputprobe status
!inputprobe capture on
!inputprobe capture off
!inputprobe off
```

## Рекомендуемый тест

После загрузки DLL:

```text
!inputprobe on
!inputprobe capture on
```

Проверить, что стандартный weapon-selection HUD скрыт, затем нажать несколько раз:

```text
5 5 5
6 6 6
7 7 7
8 8 8
9 9 9
0 0 0
```

В серверном логе ожидаются строки вида:

```text
[InputProbe][CAPTURE] ... key=5 slot=slot5 weapon=weapon_c4 ... suppressed=True
[InputProbe][CAPTURE] ... key=6 slot=slot6 weapon=weapon_hegrenade ... suppressed=True
```

При этом активное оружие игрока не должно переключаться на тестовый предмет.

После теста:

```text
!inputprobe capture off
!inputprobe off
```

Ожидается восстановление weapon HUD и удаление только временных предметов probe.
