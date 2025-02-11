## Mouse API methods

## 1. `MouseButtons`:

Available mouse buttons:

- `Left` (Left button)
- `Right` (Right\button)
- `Middle` (Middle button / wheel)

### Example of usage in `config. lua`:

```lua
local Mouse = import("TwitchController", "TwitchController.Hardware"). Mouse -- Import interface
local button = Mouse.MouseButton.Left -- Getting the "Left" button
```

## 2. Basic methods:

| Method                                                | Description                                      |
| ----------------------------------------------------- | ------------------------------------------------ |
| `MoveAsync(int x, int y)`                             | Moves the cursor to the coordinates (x, y)       |
| `ClickAsync(MouseButton button)`                      | Clicks the specified button                      |
| `HoldAsync(MouseButton button, int timeDelay = 1000)` | Holds the button for the specified time (ms)     |
| `PressAsync(MouseButton button)`                      | Presses the button (without releasing)           |
| `ReleaseAsync(MouseButton button)`                    | Releases the button                              |
| `ScrollAsync(int delta)`                              | Scrolls the wheel (delta > 0 up, delta < 0 down) |

### Usage examples in `config. lua`:

**Moving the cursor:**

```lua
Mouse.MoveAsync(500, 300) -- Move the cursor to (500, 300)
```

**Left-click:**

```lua
local button = Mouse.MouseButton.Left
Mouse.ClickAsync(button)
```

**Hold down the right button for 2 seconds:**

```lua
local button = Mouse.MouseButton.Right
Mouse.HoldAsync(button, 2000)
```

**Scrolling the wheel:**

```lua
Mouse. ScrollAsync(120) -- Scroll up
Mouse. ScrollAsync(-120) -- Scroll down
```

**Pressing and releasing the button:**

```lua
local button = Mouse.MouseButton.Middle
Mouse.PressAsync(button) - - Press the middle button
-- ... perform actions ...
Mouse.ReleaseAsync(button) - - Release
```

## 3. Features:

- For `ScrollAsync`, the value `delta` is usually a multiple of 120 (standard scroll step).
- The `HoldAsync`, `PressAsync`, `ReleaseAsync` methods work with any of the buttons: `Left`, `Right`, `Middle`.
- Coordinates in `MoveAsync` correspond to the screen resolution (for example, 1920x1080).
