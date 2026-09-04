# ElysiumInputProbe

Диагностический SwiftlyS2-плагин для исследования того, какие данные ввода CS2-клиент реально передаёт серверу.

Плагин ничего не изменяет в `CUserCmd`: только читает и логирует данные для игрока, который явно включил probe.

## Что наблюдаем

Одновременно логируются три уровня:

1. `Core.GameHooks.Controller.ProcessUsercmds.Pre`
   - `CBaseUserCmdPB.ButtonsPb` (`buttonstate1/2/3`);
   - `CInButtonState.ButtonStates[0..2]`;
   - button-события из `SubtickMoves`;
   - `Weaponselect`;
   - `Impulse`;
   - `CmdFlags`;
   - command number и client tick.
2. `Core.Event.OnClientKeyStateChanged` — то, что Swiftly уже смог преобразовать в `KeyKind`.
3. `Core.Command.HookClientCommand` — команды, которые действительно дошли от клиента до серверного command pipeline.

## Команды

```text
!inputprobe on
!inputprobe off
!inputprobe status
!inputprobe reset
!inputprobe mode changes
!inputprobe mode all
!inputprobe mark 1
```

`changes` — режим по умолчанию. USER_CMD пишется только при изменении button state / weaponselect / impulse либо при наличии button subtick.

`all` — пишет каждый `usercmd` выбранного игрока. Использовать короткими интервалами: этот режим специально шумный.

`mark <label>` — добавляет метку в последующие строки лога. Удобно перед тестированием конкретной клавиши.

## Рекомендуемый тест цифр

Стоять на месте и ничего не нажимать кроме тестируемой клавиши.

```text
!inputprobe on
!inputprobe mark key-1
```

Нажать `1` несколько раз. Затем:

```text
!inputprobe mark key-2
```

Нажать `2` несколько раз и так далее до `9`.

Отдельно проверить повторный выбор уже активного оружия: например, взять primary weapon и несколько раз нажать `1`.

Если в `changes` нажатие не оставляет следа, повторить короткий тест:

```text
!inputprobe mode all
!inputprobe mark key-1-all
```

Нажать `1` один-два раза, затем сразу вернуть:

```text
!inputprobe mode changes
```

## На что смотреть

Пример строки:

```text
[InputProbe][USERCMD] ... pb=[0x...,0x...,0x...] schema=[0x...,0x...,0x...] activeBits=20:Weapon1? changedBits=20:Weapon1?+ weaponSelect=... subticks=...
```

Главные вопросы эксперимента:

- появляется ли уникальный бит для `1`, `2`, ... `9`;
- появляются ли button subticks;
- меняется ли `weaponSelect`;
- видит ли тот же ввод `OnClientKeyStateChanged`;
- приходит ли `slot1`, `slot2`, ... как client command;
- есть ли след при повторном нажатии цифры, когда соответствующее оружие уже выбрано.

Если цифры имеют стабильный уникальный server-side след, следующий шаг — вынести его в нормальный `ElysiumMenu` input adapter вместо diagnostic probe.
