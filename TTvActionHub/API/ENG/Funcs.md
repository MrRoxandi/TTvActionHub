# Documentation for the 'Funcs' Module

This module provides a set of general helper functions, including random number and string generation, execution delays, working with collections, and generating random positions.

Example of using `RandomNumber` to get a random number and `Delay` for a pause:

```lua
-- Get a random number from 1 to 10 inclusive
local randomNumber = Funcs.RandomNumber(1, 10)
print('Random number: ' .. randomNumber)

-- Create a random delay from 500 to 1500 ms
local randomDelay = Funcs.RandomNumber(500, 1500)
print('Pausing for ' .. randomDelay .. ' ms...')
Funcs.Delay(randomDelay) -- Pause script execution
print('Pause finished.')

```

### Available Methods

Below is a list of available methods in the `Funcs` module.

| Method                                                   | Description                                                                                                                                   |
|----------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| `RandomNumber(int min, int max)`                         | Returns a random integer in the range [`min`,`max`] (inclusive).                                                                              |
| `RandomDouble(double min, double max)`                   | Returns a random floating-point number in the range [`min` ,`max`) (inclusive of min, exclusive of max).                                      |
| `RandomElement(table elements)`                          | Returns a random string from the provided list. If the list is empty, returns an empty string.                                                |
| `Shuffle(table elements)`                                | Shuffles the elements in the provided list and returns a **new** list. If the list is empty, returns an empty list.                           |
| `RandomString(int length)`                               | Generates a random string of the specified length from Latin letters and digits (0-9).                                                        |
| `Delay(int delayMs)`                                     | Pauses the execution of the current script for the specified number of **milliseconds**.                                                      |
| `RandomPosition(int minX, int maxX, int minY, int maxY)` | Returns an object with `X` and `Y` fields, containing random coordinates within the specified ranges.                                         |
| `CollectionToString(table elements, string separator)`   | Joins the elements of a list of strings into a single string, using the specified `separator`. If the list is empty, returns an empty string. |

Example of using `RandomPosition` and `CollectionToString`:

```lua
-- Get a random position in a rectangle from (100, 100) to (500, 500)
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