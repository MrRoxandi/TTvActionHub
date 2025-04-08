## Documentation for the 'Mouse' module in `TTvActionHub.LuaTools.Hardware`

This module provides functions for emulating mouse actions, such as moving the cursor, clicking, and scrolling.

### Connecting in the configuration file

Module connection example:

```lua
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse
```

Example of getting the code for the left mouse button and simulating a single click:

```lua
local button = Mouse.Button.Left -- Get the code for the left mouse button
Mouse.Click(button)
```

### Available mouse buttons for simulation:

Currently, all available standard buttons are located in the `Button` field of the `Mouse` module. Below is a list of them:

1.  **Standard mouse buttons**:
    `Left`, `Right`, `Middle`
2.  **Additional mouse buttons**:
    Additional mouse buttons are handled specially and do not have explicit names (values). They are referenced by an ID number.

### Available mouse simulation methods

Methods for interacting with the main mouse buttons:

| Method                              | Description                                                      |
| ----------------------------------- | ---------------------------------------------------------------- |
| `Press(Button button)`              | Simulates pressing the `button` (but does not release it)        |
| `Release(Button button)`            | Simulates releasing the `button`                                 |
| `Click(Button button)`              | Simulates a quick single press and release of the `button`       |
| `Hold(Button button, int duration)` | Simulates holding down the `button` for the specified `duration` |

Methods for interacting with additional mouse buttons, where `xid` is the number of the additional button starting from **1**:

| Method                         | Description                                                       |
| ------------------------------ | ----------------------------------------------------------------- |
| `XPress(int xid)`              | Simulates pressing the additional button `xid` (does not release) |
| `XRelease(int xid)`            | Simulates releasing the additional button `xid`                   |
| `XClick(int xid)`              | Simulates a quick single press and release of button `xid`        |
| `XHold(int xid, int duration)` | Simulates holding down the additional button `xid`                |

Methods for interacting with the mouse pointer:

| Method                      | Description                                                 |
| --------------------------- | ----------------------------------------------------------- |
| `SetPosition(int x, int y)` | Simulates setting the cursor to the specified coordinates   |
| `Move(int dx, int dy)`      | Simulates moving the cursor by the specified offset amounts |

**Clarification**: The coordinates for the pointer correspond to your monitor's resolution.

Methods for interacting with the mouse wheel (scrolling):

| Method                  | Description                    |
| ----------------------- | ------------------------------ |
| `VScroll(int distance)` | Simulates vertical scrolling   |
| `HScroll(int distance)` | Simulates horizontal scrolling |

Example using 'SetPosition' in the configuration file:

```lua
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse
Mouse.SetPosition(560, 20)
```
