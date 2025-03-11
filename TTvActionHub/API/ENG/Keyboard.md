## Documentation for the Keyboard Module in `TTvActionHub.LuaTools.Hardware`

### Keys, or rather key codes...

The following keys are currently available:

1. **Function keys:**
   `F1`,`F2`,`F3`,`F4`,`F5`,`F6`,`F7`,`F8`,`F9`,`F10`,`F11`,`F12`
2. **Alphabet:**
   `A`, `B`, `C`, `D`, `E`, `F`, `G`, `H`, `I`, `J`, `K`, `L`, `M`, `N`, `O`, `P`, `Q`, `R`, `S`, `T`, `U`, `V`, `W`, `X`, `Y`, `Z`
3. **Numeric keypad:**
   `NUM_0`, `NUM_1`, `NUM_2`, `NUM_3`, `NUM_4`, `NUM_5`, `NUM_6`, `NUM_7`, `NUM_8`, `NUM_9`
4. **Special keys:**
   `ENTER`, `ESCAPE`, `BACKSPACE`, `TAB`, `SPACE`, `SHIFT`, `CONTROL`, `ALT`, `CAPS_LOCK`
5. **Arrows:**
   `LEFT`, `UP`, `RIGHT`, `DOWN`

Example usage in `config.lua`

```lua
local Keyboard = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Keyboard

local key = Keyboard.KeyCodes.B -- Getting the key code for 'B'
```

## Keyboard Emulation

| Method                                        | Description                                                  |
| --------------------------------------------- | ------------------------------------------------------------ |
| `PressKey(Key key)`                           | Simulates pressing a key (does not release it)               |
| `ReleaseKey(Key key)`                         | Releases a pressed key                                       |
| `TypeKeyAsync(Key key)`                       | Simulates a short key press (press and release)              |
| `HoldKeyAsync(Key key, int timeDelay = 1000)` | Holds down a key for the specified time (default is 1000 ms) |

Example usage in `config.lua`

```lua
local Keyboard = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Keyboard

local key = Keyboard.KeyCodes.B -- Getting the key code for 'B'

Keyboard.TypeKeyAsync(key) -- Simulating a short press of the 'B' key
```
