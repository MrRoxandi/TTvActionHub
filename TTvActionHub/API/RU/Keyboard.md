## Документация для модуля Keyboard в `TTvActionHub.LuaTools.Hardware`

### Клавиши, или же ключи...

На текущий момент времени доступны следующие клавиши:

1. **Функциональные клавиши:**
   `F1`,`F2`,`F3`,`F4`,`F5`,`F6`,`F7`,`F8`,`F9`,`F10`,`F11`,`F12`
2. **Алфавит:**
   `A`, `B`, `C`, `D`, `E`, `F`, `G`, `H`, `I`, `J`, `K`, `L`, `M`, `N`, `O`, `P`, `Q`, `R`, `S`, `T`, `U`, `V`, `W`, `X`, `Y`, `Z`
3. **Цифровая клавиатура:**
   `NUM_0`, `NUM_1`, `NUM_2`, `NUM_3`, `NUM_4`, `NUM_5`, `NUM_6`, `NUM_7`, `NUM_8`, `NUM_9`
4. **Специальные клавиши:**
   `ENTER`, `ESCAPE`, `BACKSPACE`, `TAB`, `SPACE`, `SHIFT`, `CONTROL`, `ALT`, `CAPS_LOCK`
5. **Стрелки:**
   `LEFT`, `UP`, `RIGHT`, `DOWN`

Пример использования в `config.lua`

```lua
local Keyboard = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Keyboard

local key = Keyboard.KeyCodes.B -- Получение кода клавиши 'B'
```

## Эмуляция клавиатуры

| Метод                                         | Описание                                                              |
| --------------------------------------------- | --------------------------------------------------------------------- |
| `PressKey(Key key)`                           | Имитирует нажатие клавиши (не отпускает обратно)                      |
| `ReleaseKey(Key key)`                         | Отпускает нажатую клавишу                                             |
| `TypeKeyAsync(Key key)`                       | Имитирует краткое нажатие на клавишу (нажал, отпустил)                |
| `HoldKeyAsync(Key key, int timeDelay = 1000)` | Удерживает клавишу в течение заданного времени (по умолчанию 1000 мс) |

Пример использования в `config.lua`

```lua
local Keyboard = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Keyboard

local key = Keyboard.KeyCodes.B -- Получение кода клавиши 'B'

Keyboard.TypeKeyAsync(key) -- Имитация краткого нажатия клавиши 'B'
```
