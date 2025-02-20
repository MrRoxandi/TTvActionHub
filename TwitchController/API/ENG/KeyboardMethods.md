## Keyboard API Methods

## 1. `KeyCodes`:

Available keys:

- Function keys:
  `F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12`.

- Alphabet:
  `A, B, C, D, E, F, G, H, I, J, K, L, M, M, N, N, O, P, Q, R, S, T, U, V, W, X, Y, Z`.

- Numeric keypad:
  `NUM_0, NUM_1, NUM_2, NUM_3, NUM_4, NUM_5, NUM_6, NUM_7, NUM_8, NUM_9`.

- Special keys:
  `ENTER, ESCAPE, BACKSPACE, TAB, SPACE, SHIFT, CONTROL, ALT, CAPS_LOCK`.

- Arrows:
  `LEFT, UP, RIGHT, DOWN`.

### Example usage in `config.lua`:

```lua
local Keyboard = import("TwitchController", "TwitchController.Hardware").Keyboard -- Import Interface

local key = Keyboard.KeyCodes.B -- Get the "B" key code.
```

---

## 2. `Virtual Keys`

| Method                                            | Description                                               |
| ------------------------------------------------- | --------------------------------------------------------- |
| `PressKey(KeyCode key)`                           | Presses a key and holds it down                           |
| `ReleaseKey(KeyCode key)`                         | Releases the pressed key                                  |
| `TypeKeyAsync(KeyCode key)`                       | Presses and releases a key                                |
| `HoldKeyAsync(KeyCode key, int timeDelay = 1000)` | Holds the key for the specified time (default is 1000 ms) |

### Example usage in `config.lua`:

```lua
local Keyboard = import("TwitchController", "TwitchController.Hardware").Keyboard -- Import the interface

local key = Keyboard.KeyCodes.B -- Get the key code "B".

Keyboard.TypeKeyAsync(key) -- Simulate pressing the "B" key
```
