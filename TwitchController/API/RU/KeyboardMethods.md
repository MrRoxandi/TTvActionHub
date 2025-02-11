## Keyboard API Methods

## 1. `KeyCodes`:

Доступные клавиши:

- **Функциональные клавиши:**
  `F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12`

- **Алфавит:**
  `A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z`

- **Цифровая клавиатура:**
  `NUM_0, NUM_1, NUM_2, NUM_3, NUM_4, NUM_5, NUM_6, NUM_7, NUM_8, NUM_9`

- **Специальные клавиши:**
  `ENTER, ESCAPE, BACKSPACE, TAB, SPACE, SHIFT, CONTROL, ALT, CAPS_LOCK`

- **Стрелки:**
  `LEFT, UP, RIGHT, DOWN`

### Пример использования в `config.lua`:

```lua
local Keyboard = import('TwitchController', 'TwitchController.Hardware').Keyboard -- Импорт интерфейса

local key = Keyboard.KeyCodes.B -- Получение кода клавиши 'B'
```

---

## 2. `Виртуальные клавиши`

| Метод                                             | Описание                                                              |
| ------------------------------------------------- | --------------------------------------------------------------------- |
| `PressKey(KeyCode key)`                           | Нажимает клавишу и удерживает её                                      |
| `ReleaseKey(KeyCode key)`                         | Отпускает нажатую клавишу                                             |
| `TypeKeyAsync(KeyCode key)`                       | Нажимает и сразу отпускает клавишу                                    |
| `HoldKeyAsync(KeyCode key, int timeDelay = 1000)` | Удерживает клавишу в течение заданного времени (по умолчанию 1000 мс) |

### Пример использования в `config.lua`:

```lua
local Keyboard = import('TwitchController', 'TwitchController.Hardware').Keyboard -- Импорт интерфейса

local key = Keyboard.KeyCodes.B -- Получение кода клавиши 'B'

Keyboard.TypeKeyAsync(key) -- Имитация нажатия клавиши 'B'
```
