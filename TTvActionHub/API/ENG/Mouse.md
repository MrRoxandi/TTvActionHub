## Documentation for the Mouse Module in `TTvActionHub.LuaTools.Hardware`

This module provides functions for emulating mouse actions such as moving the cursor, clicks, and scrolling.

### Types

#### `MouseButton`

An enumeration representing the mouse buttons.

| Value    | Description   |
| -------- | ------------- |
| `Left`   | Left button   |
| `Right`  | Right button  |
| `Middle` | Middle button |

### Functions

| Function                                                             | Description                                                                                                                                                                                                                                          |
| -------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SetPosition(int x, int y)`                                          | Sets the mouse cursor position to the specified coordinates `x` and `y` on the screen.                                                                                                                                                               |
| `MoveAsync(int targetX, int targetY, int steps = 50, int delay = 5)` | Asynchronously moves the mouse cursor from the current position to the specified coordinates `targetX` and `targetY`. The `steps` parameter determines the number of steps for the movement, and `delay` is the delay in milliseconds between steps. |
| `HoldAsync(MouseButton button, int timeDelay = 1000)`                | Asynchronously holds the specified mouse button (`button`) pressed for the given time (`timeDelay` in milliseconds), and then releases it.                                                                                                           |
| `Click(MouseButton button)`                                          | Simulates a single click of the specified mouse button (`button`).                                                                                                                                                                                   |
| `Press(MouseButton button)`                                          | Simulates pressing the specified mouse button (`button`) (does not release it).                                                                                                                                                                      |
| `Release(MouseButton button)`                                        | Simulates releasing the pressed mouse button (`button`).                                                                                                                                                                                             |
| `Scroll(int delta)`                                                  | Simulates scrolling with the mouse wheel. The `delta` value determines the direction and amount of scrolling. A positive value scrolls up, a negative value scrolls down.                                                                            |

**Notes:**

- All functions ending with `Async` are executed asynchronously, without blocking the main program thread.
- The `x` and `y` coordinates in the `SetPosition` and `MoveAsync` functions represent coordinates on the screen.
- To use functions with mouse buttons, you must specify the corresponding value from the `MouseButton` enumeration.

### Example usage in `config.lua`

```lua
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse

-- Setting the cursor position
Mouse.SetPosition(100, 200)

-- Asynchronously moving the cursor with a smooth transition
Mouse.MoveAsync(500, 500, 100, 10)

-- Clicking the left mouse button
Mouse.Click(Mouse.MouseButton.Left)

-- Holding the right mouse button for 2 seconds
Mouse.HoldAsync(Mouse.MouseButton.Right, 2000)

-- Scrolling down
Mouse.Scroll(-120)
```
