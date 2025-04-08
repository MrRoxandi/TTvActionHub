## Documentation for the 'Funcs' module in `TTvActionHub.LuaTools.Stuff`

This module provides a set of general helper functions, including generating random numbers and strings, execution delays, working with collections, and generating random positions.

### Connecting in the configuration file

Module connection example:

```lua
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs
```

Example using `RandomNumber` to get a random number and `Delay` for a pause:

```lua
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs

-- Get a random integer from 1 to 10 inclusive
local randomNumber = Funcs.RandomNumber(1, 10)
print('Random number: ' .. randomNumber)

-- Create a random delay between 500 and 1500 ms
local randomDelay = Funcs.RandomNumber(500, 1500)
print('Pausing for ' .. randomDelay .. ' ms...')
Funcs.Delay(randomDelay) -- Pause script execution
print('Pause finished.')

```

### Available methods

Below is a list of the `Funcs` module's available methods.

| Method                                                   | Description                                                                                                                                                   |
| -------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `RandomNumber(int min, int max)`                         | Returns a random integer in the inclusive range [`min`, `max`].                                                                                               |
| `RandomDouble(double min, double max)`                   | Returns a random floating-point number (double) in the range [`min`, `max`).                                                                                  |
| `RandomElement(table elements)`                          | Returns a random element from the provided table. If the table is empty, returns an empty string.                                                             |
| `Shuffle(table elements)`                                | Shuffles the elements in the provided table and returns a **new** table. If the table is empty, returns an empty table.                                       |
| `RandomString(int length)`                               | Generates a random string of the specified length using Latin letters and digits (0-9).                                                                       |
| `Delay(int delayMs)`                                     | Pauses the execution of the current script for the specified number of **milliseconds**.                                                                      |
| `RandomPosition(int minX, int maxX, int minY, int maxY)` | Returns an object (table) with `X` and `Y` fields containing random coordinates within the specified ranges.                                                  |
| `CollectionToString(table elements, string separator)`   | Joins the elements of a table (expected to be strings) into a single string, using the specified `separator`. If the table is empty, returns an empty string. |

Example using `RandomPosition` and `CollectionToString`:

```lua
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs
local Mouse = import('TTvActionHub', 'TTvActionHub.LuaTools.Hardware').Mouse -- Assuming the Mouse module is also connected

-- Get a random position within the rectangle from (100, 100) to (500, 500)
local pos = Funcs.RandomPosition(100, 500, 100, 500)
print('Random position: X=' .. pos.X .. ', Y=' .. pos.Y)

-- Set the cursor to this random position
Mouse.SetPosition(pos.X, pos.Y)

-- Example of working with a collection
local myList = { 'apple', 'banana', 'orange' }
local shuffledList = Funcs.Shuffle(myList) -- Shuffle the list

-- Print the shuffled list, joined by a comma and space
local resultString = Funcs.CollectionToString(shuffledList, ', ')
print('Shuffled list: ' .. resultString)

-- Select a random fruit from the original list
local randomFruit = Funcs.RandomElement(myList)
print('Random fruit: ' .. randomFruit)
```
