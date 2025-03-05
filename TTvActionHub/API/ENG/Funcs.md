## Documentation for the Funcs Module in `TTvActionHub.LuaTools.Stuff`

This module provides a set of useful functions for performing various operations, such as generating random numbers, selecting a random element from a collection, shuffling a collection, and creating random strings.

### Functions

| Function                                                                     | Description                                                                                                                                                                                                        | Return Value   |
| ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------- |
| `RandomNumber(int? min, int? max)`                                           | Generates a random integer within the range from `min` to `max` (inclusive). Both arguments `min` and `max` are **required**.                                                                                      | `int`          |
| `RandomDouble(double? min, double? max)`                                     | Generates a random floating-point number within the range from `min` to `max`. Both arguments `min` and `max` are **required**.                                                                                    | `double`       |
| `RandomNumberAsync(int? min, int? max)`                                      | Asynchronously generates a random integer within the range from `min` to `max` (inclusive). Both arguments `min` and `max` are **required**.                                                                       | `int`          |
| `RandomDoubleAsync(double? min, double? max)`                                | Asynchronously generates a random floating-point number within the range from `min` to `max`. Both arguments `min` and `max` are **required**.                                                                     | `double`       |
| `RandomElementAsync(IEnumerable<string>? collection)`                        | Asynchronously selects a random element from the provided string collection `collection`. If the collection is empty, returns an empty string. The argument `collection` is **required**.                          | `string`       |
| `ShuffleAsync(IEnumerable<string>? collection)`                              | Asynchronously shuffles the provided string collection `collection` and returns a new shuffled collection as a list. If the collection is empty, returns an empty list. The argument `collection` is **required**. | `List<string>` |
| `RandomStringAsync(int length)`                                              | Asynchronously generates a random string of the specified length `length`, consisting of letters (uppercase and lowercase) and digits.                                                                             | `string`       |
| `DelayAsync(int? delay)`                                                     | Asynchronously pauses execution for the specified number of milliseconds `delay`. The argument `delay` is **required**.                                                                                            | `void`         |
| `RandomPositionAsync(int? minX, int? maxX, int? minY, int? maxY)`            | Asynchronously generates a random position (Point) with random X and Y coordinates within the specified ranges. All arguments are **required**.                                                                    | `Funcs.Point`  |
| `CollectionToStringAsync(IEnumerable<string>? collection, string sep = " ")` | Asynchronously converts a string collection `collection` into a single string, separating the elements with the specified separator `sep` (default is a space). The argument `collection` is **required**.         | `string`       |

### Types

#### `Point`

A structure representing a point with X and Y coordinates.

| Property | Type  | Description  |
| -------- | ----- | ------------ |
| `X`      | `int` | X coordinate |
| `Y`      | `int` | Y coordinate |

**Notes:**

- All functions ending with `Async` are executed asynchronously, without blocking the main program thread.
- Before using any function, ensure that all required arguments are provided. Otherwise, an `ArgumentNullException` will be thrown.
- When working with `RandomPositionAsync`, all four parameters must be specified: `minX`, `maxX`, `minY`, `maxY`.
- To get the result from asynchronous functions, you need to use `.Result`. For example: `local randomNumber = Funcs.RandomNumberAsync(1, 100).Result`

### Example usage in `config.lua`

```lua
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs

-- Generating a random number from 1 to 100
local randomNumber = Funcs.RandomNumber(1, 100)
print("Random number: " .. randomNumber)

-- Generating a random floating-point number from 0.0 to 1.0
local randomDouble = Funcs.RandomDouble(0.0, 1.0)
print("Random floating-point number: " .. randomDouble)

-- Selecting a random element from a list
local myList = {"apple", "banana", "cherry"}
local randomElement = Funcs.RandomElementAsync(myList).Result
print("Random element: " .. randomElement)


-- Shuffling a list
local myList = {"apple", "banana", "cherry"}
local shuffledList = Funcs.ShuffleAsync(myList).Result
print("Shuffled list:")
for i, element in ipairs(shuffledList) do
    print(i .. ": " .. element)
end


-- Generating a random string of 10 characters
local randomString = Funcs.RandomStringAsync(10).Result
print("Random string: " .. randomString)


-- Pausing execution for 1 second
Funcs.DelayAsync(1000).Result

-- Getting a random position
local pos = Funcs.RandomPositionAsync(0, 100, 0, 100).Result
print("Random position: X=" .. pos.X .. ", Y=" .. pos.Y)


-- Converting a list to a string with a separator
local myList = {"apple", "banana", "cherry"}
local myString = Funcs.CollectionToStringAsync(myList, ", ").Result
print("String: " .. myString)
```
