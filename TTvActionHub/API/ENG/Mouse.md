# Documentation for the 'Mouse' module in `TTvActionHub.LuaTools.Hardware`

This module provides functions for emulating mouse actions, such as cursor movement, clicks, and scrolling.

## Importing into the configuration file

Example of importing the module:

```lua
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse
```

Example of getting the code for the left mouse button and simulating a click of this button:

```lua
local button = Mouse.Button.Left -- Get the code for the left mouse button
Mouse.Click(button)
```

## Available mouse buttons for simulation

Currently, all available buttons are located in the `Button` field of the `Mouse` module. Below is a list of them:

1. **Standard mouse buttons**:
   `Left`, `Right`, `Middle`
2. **Additional mouse buttons**:
   Additional mouse buttons are handled in a special way and do not have explicit names (values). They are referred to by an ID.

## Available mouse simulation methods

Methods for interacting with main mouse buttons:

| Method                               | Description                                                                          |
|:-------------------------------------|:-------------------------------------------------------------------------------------|
| `Press(Button button)`               | Simulates pressing the `button` (but does not release it).                           |
| `Release(Button button)`             | Simulates releasing the `button`.                                                    |
| `Click(Button button)`               | Simulates a quick, single click of the `button`.                                     |
| `Hold(Button button, int duration)`  | Simulates holding down the `button` for the specified `duration` (in milliseconds).  |

Methods for interacting with additional mouse buttons, where `xid` is the number of the additional button, starting from **1**:

| Method                         | Description                                                                             |
|--------------------------------|-----------------------------------------------------------------------------------------|
| `XPress(int xid)`              | Simulates pressing the `xid` button (but does not release it).                          |
| `XRelease(int xid)`            | Simulates releasing the `xid` button.                                                   |
| `XClick(int xid)`              | Simulates a quick, single click of the `xid` button.                                    |
| `XHold(int xid, int duration)` | Simulates holding down the `xid` button for the specified `duration` (in milliseconds). |

Methods for interacting with the mouse pointer:

| Method                      | Description                                                                  |
|-----------------------------|------------------------------------------------------------------------------|
| `SetPosition(int x, int y)` | Simulates setting the cursor to the specified coordinates.                   |
| `Move(int dx, int dy)`      | Simulates moving the cursor by the specified delta coordinates (`dx`, `dy`). |

**Clarification**: The coordinates for the pointer are based on your monitor's resolution.

Methods for interacting with the mouse wheel (scrolling):

| Method                  | Description                                                 |
|-------------------------|-------------------------------------------------------------|
| `VScroll(int distance)` | Simulates vertical scrolling by the specified `distance`.   |
| `HScroll(int distance)` | Simulates horizontal scrolling by the specified `distance`. |

Example of using `SetPosition` in the configuration file:

```lua
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse
Mouse.SetPosition(560, 20)
```
