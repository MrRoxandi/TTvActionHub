# Documentation for the 'Keyboard' Module

Example of getting the key code for 'B' and pressing this key once:

```lua
local key = Keyboard.Key("B") -- Get the key code for 'B'
Keyboard.TypeKey(key)
```

## Available Keys for Simulation

Currently, all available keys are in the `Key` field of the `Keyboard` module. Below is a list of them:

1.  **Number Row**:
    `D0`, `D1`, `D2`, `D3`, `D4`, `D5`, `D6`, `D7`, `D8`, `D9`
2.  **Alphabet**:
    `A`, `B`, `C`, `D`, `E`, `F`, `G`, `H`, `I`, `J`, `K`, `L`,`M`,
    `N`, `O`, `P`, `Q`, `R`, `S`, `T`, `U`, `V`, `W`, `X`, `Y`, `Z`
3.  **Numeric Keypad**:
    `NumLock`, `NumPad0`, `NumPad1`, `NumPad2`, `NumPad3`, `NumPad4`,
    `NumPad5`, `NumPad6`, `NumPad7`, `NumPad8`, `NumPad9`, `Multiply`,
    `Separator`, `Add`, `Subtract`, `Decimal`, `Divide`
4.  **Function Keys:**
    `F1`, `F2`, `F3`, `F4`, `F5`, `F6`, `F7`, `F8`, `F9`,
    `F10`, `F11`, `F12`, `F13`, `F14`, `F15`, `F16`, `F17`,
    `F18`, `F19`, `F20`, `F21`, `F22`, `F23`, `F24`
5.  **Special Keys**:
    `Shift`, `RShiftKey`, `LShiftKey`, `Alt`, `LAlt`, `RAlt`, `Control`, `LControlKey`, `RControlKey`
6.  **Additional Keys**:
    `LWin`, `RWin`, `Backspace`, `Tab`, `LineFeed`, `Clear`, `Enter`, `Pause`, `CapsLock`, `Escape`, `Space`, `PageUp`, `PageDown`, `End`, `Home`, `PrintScreen`, `Insert`, `Delete`, `Scroll`, `Sleep`,
7.  **Arrow Keys**:
    `Up`, `Right`, `Left`, `Down`

## Available Keyboard Simulation Methods

| Method                                   | Description                                                                  |
|------------------------------------------|------------------------------------------------------------------------------|
| `PressKey(Key key)`                      | Simulates pressing a key (does not release it)                               |
| `ReleaseKey(Key key)`                    | Releases a pressed key                                                       |
| `TypeKey(Key key)`                       | Simulates a short key press (press, release)                                 |
| `TypeMessage(string message)`            | Simulates typing a message from the given string                             |
| `HoldKey(Key key, int timeDelay = 1000)` | Holds a key down for a specified duration (default 1000 ms)                  |

Example of using `TypeMessage` in the configuration file:

```lua
Keyboard.TypeMessage('Example text!') -- Emulates physical typing of 'Example text!'
```