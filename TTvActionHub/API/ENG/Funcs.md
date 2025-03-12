## Documentation for the Funcs Module in `TTvActionHub.LuaTools.Stuff`

This module provides a set of utility functions for performing various operations such as generating random numbers, selecting random elements from a collection, shuffling a collection, and creating random strings.

### Functions

| Function                                                                  | Description                                                                                                                                                                                                 | Return Value   |
| ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------- |
| `RandomNumber(int? min, int? max)`                                        | Generates a random integer within the range from `min` to `max` (inclusive). Both arguments, `min` and `max`, are **required**.                                                                             | `int`          |
| `RandomDouble(double? min, double? max)`                                  | Generates a random floating-point number within the range from `min` to `max`. Both arguments, `min` and `max`, are **required**.                                                                           | `double`       |
| `RandomNumberAsync(int? min, int? max)`                                   | Asynchronously generates a random integer within the range from `min` to `max` (inclusive). Both arguments, `min` and `max`, are **required**.                                                              | `int`          |
| `RandomDoubleAsync(double? min, double? max)`                             | Asynchronously generates a random floating-point number within the range from `min` to `max`. Both arguments, `min` and `max`, are **required**.                                                            | `double`       |
| `RandomElement(IEnumerable<string>? elemets)`                             | Selects a random element from the provided string collection `elemets`. Returns an empty string if the collection is empty. The `elemets` argument is **required**.                                         | `string`       |
| `RandomElementAsync(IEnumerable<string>? elemets)`                        | Asynchronously selects a random element from the provided string collection `elemets`. Returns an empty string if the collection is empty. The `elemets` argument is **required**.                          | `string`       |
| `Shuffle(IEnumerable<string>? elemets)`                                   | Shuffles the provided string collection `elemets` and returns a new shuffled collection as a list. Returns an empty list if the collection is empty. The `elemets` argument is **required**.                | `List<string>` |
| `ShuffleAsync(IEnumerable<string>? elemets)`                              | Asynchronously shuffles the provided string collection `elemets` and returns a new shuffled collection as a list. Returns an empty list if the collection is empty. The `elemets` argument is **required**. | `List<string>` |
| `RandomString(int length)`                                                | Generates a random string of the specified `length`, consisting of letters (uppercase and lowercase) and numbers.                                                                                           | `string`       |
| `RandomStringAsync(int length)`                                           | Asynchronously generates a random string of the specified `length`, consisting of letters (uppercase and lowercase) and numbers.                                                                            | `string`       |
| `DelayAsync(int? delay)`                                                  | Asynchronously pauses execution for the specified number of milliseconds `delay`. The `delay` argument is **required**.                                                                                     | `void`         |
| `RandomPosition(int? minX, int? maxX, int? minY, int? maxY)`              | Generates a random position (Point) with random X and Y coordinates within the specified ranges. All arguments are **required**.                                                                            | `Funcs.Point`  |
| `RandomPositionAsync(int? minX, int? maxX, int? minY, int? maxY)`         | Asynchronously generates a random position (Point) with random X and Y coordinates within the specified ranges. All arguments are **required**.                                                             | `Funcs.Point`  |
| `CollectionToString(IEnumerable<string>? elemets, string sep = " ")`      | Converts a string collection `elemets` into a single string, separating elements with the specified separator `sep` (default is a space). The `elemets` argument is **required**.                           | `string`       |
| `CollectionToStringAsync(IEnumerable<string>? elemets, string sep = " ")` | Asynchronously converts a string collection `elemets` into a single string, separating elements with the specified separator `sep` (default is a space). The `elemets` argument is **required**.            | `string`       |

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
- To work with `RandomPositionAsync`, all four parameters must be specified: `minX`, `maxX`, `minY`, `maxY`.
- To retrieve the result from asynchronous functions, you must use `.Result`. For example: `local randomNumber = Funcs.RandomNumberAsync(1, 100).Result`

### Example Usage in `config.lua`

```lua
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs

-- Generate a random number from 1 to 100
local randomNumber = Funcs.RandomNumber(1, 100)
print("Random number: " .. randomNumber)

-- Generate a random floating-point number from 0.0 to 1.0
local randomDouble = Funcs.RandomDouble(0.0, 1.0)
print("Random floating-point number: " .. randomDouble)

-- Select a random element from a list
local myList = {"apple", "banana", "cherry"}
local randomElement = Funcs.RandomElementAsync(myList).Result
print("Random element: " .. randomElement)


-- Shuffle a list
local myList = {"apple", "banana", "cherry"}
local shuffledList = Funcs.ShuffleAsync(myList).Result
print("Shuffled list:")
for i, element in ipairs(shuffledList) do
    print(i .. ": " .. element)
end


-- Generate a random string of 10 characters
local randomString = Funcs.RandomStringAsync(10).Result
print("Random string: " .. randomString)


-- Pause execution for 1 second
Funcs.DelayAsync(1000).Result

-- Get a random position
local pos = Funcs.RandomPositionAsync(0, 100, 0, 100).Result
print("Random position: X=" .. pos.X .. ", Y=" .. pos.Y)


-- Convert a list to a string with a separator
local myList = {"apple", "banana", "cherry"}
local myString = Funcs.CollectionToStringAsync(myList, ", ").Result
print("String: " .. myString)
```
