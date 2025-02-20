## Tools API methods

## 1. `RandomNumber`:

Random number generation.

- `randomNumber(int min, int max)`: Returns a random integer in the range [`min`, `max`].
- `RandomDouble(double min, double max)`: Returns a random floating-point number in the range [`min`, `max`].
- `RandomNumberAsync(int min, int max, int delay = 0)`: Asynchronously returns a random integer in the range [`min`, `max`], with a possible delay.
- `RandomDoubleAsync(double min, double max)`: Asynchronously returns a random floating-point number.

### Usage examples in `config.lua`:

```lua
local num = Tools.randomNumber(1, 100) -- Generate a random number from 1 to 99
local numAsync = Tools.RandomNumberAsync(1, 100, 500) -- Asynchronous generation with a delay of 500 ms
```

## 2. `RandomElement`:

Selecting a random item from the list.

- `RandomElementAsync<T>(IEnumerable<T> collection)`: Asynchronously selects a random item from the list.

### Example:

```lua
local list = {"apple", "banana", "cherry"}
local fruit = Tools.RandomElementAsync(list)
```

## 3. `Shuffle`:

Shuffling the list.

- `ShuffleAsync<T>(IEnumerable<T> collection)`: Asynchronously shuffles the list items.

### Example:

```lua
local list = {1, 2, 3, 4, 5}
local shuffled = Tools.ShuffleAsync(list)
```

## 4. `RandomString`:

Generating a random string.

- `RandomStringAsync(int length)`: Asynchronously creates a random string of a given length of letters and numbers.

### Example:

```lua
local randomStr = Tools.RandomStringAsync(10) -- A string of 10 random characters
```

## 5. `RandomDelay`:

Creating a random delay before performing the next action.

- `RandomDelayAsync(int minMs, int maxMs)`: Asynchronously waits for a random number of milliseconds in a given range.

### Example:

```lua
Tools.RandomDelayAsync(500, 2000) -- Delay from 500 to 2000 ms
```

## 7. `RandomPosition`:

Generating random coordinates within a rectangular area.

- `RandomPositionAsync(int minX, int maxX, int minY, int maxY)`: Asynchronously returns random coordinates within the specified boundaries.

### Example:

```lua
local x, y = Tools.RandomPositionAsync(0, 1920, 0, 1080) -- Coordinates within the screen are 1920x1080
```
