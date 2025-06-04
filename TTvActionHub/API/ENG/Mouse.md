# Documentation for the 'Mouse' Module

This module provides functions for emulating mouse actions, such as cursor movement, clicks, and scrolling.

Example of getting the left mouse button and clicking it once:

```lua
local button = Mouse.Button("Left") -- Get the code for the left mouse button
Mouse.Click(button)
```

## Available Mouse Buttons for Simulation

Currently, all available buttons are in the `Button` field of the `Mouse` module. Below is a list of them:

1.  **Standard Mouse Buttons**:
    `Left`, `Right`, `Middle`
2.  **Additional Mouse Buttons**:
    Additional mouse buttons are handled specially and do not have explicit names (values).

## Available Mouse Simulation Methods

Methods for interacting with the main mouse buttons:

| Method                              | Description                                                    |
|-------------------------------------|----------------------------------------------------------------|
| `Press(Button button)`              | Simulates pressing the `button` (but does not release it)      |
| `Release(Button button)`            | Simulates releasing the `button`                               |
| `Click(Button button)`              | Simulates a quick single click of the `button`                 |
| `Hold(Button button, int duration)` | Simulates holding down the `button` for a specified `duration` |

Methods for interacting with additional mouse buttons, where `xid` is the number of the additional button starting from **1**:

| Method                         | Description                                                        |
|--------------------------------|--------------------------------------------------------------------|
| `XPress(int xid)`              | Simulates pressing the `xid` button (but does not release it)      |
| `XRelease(int xid)`            | Simulates releasing the `xid` button                               |
| `XClick(int xid)`              | Simulates a quick single click of the `xid` button                 |
| `XHold(int xid, int duration)` | Simulates holding down the `xid` button for a specified `duration` |

Methods for interacting with the mouse pointer:

| Method                      | Description                                                          |
|-----------------------------|----------------------------------------------------------------------|
| `SetPosition(int x, int y)` | Simulates setting the cursor to the specified coordinates            |
| `Move(int dx, int dy)`      | Simulates moving the cursor by the specified coordinate displacement |

**Clarification**: The coordinates for the pointer will be based on your monitor's resolution.

Methods for interacting with the mouse wheel (scrolling):

| Method                  | Description                        |
|-------------------------|------------------------------------|
| `VScroll(int distance)` | Simulates vertical scrolling       |
| `HScroll(int distance)` | Simulates horizontal scrolling     |

Example of using 'SetPosition' in the configuration file:

```lua
Mouse.SetPosition(560, 20)
```