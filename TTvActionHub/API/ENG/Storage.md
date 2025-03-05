## Documentation for the Storage Module in `TTvActionHub.LuaTools.Stuff`

This module provides access to internal data storage, allowing scripts to save and retrieve information.

### Functions

| Function                                       | Description                                                                                                                                       | Return Value |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- | ------------ |
| `Contains(string name)`                        | Checks if a value with the specified name exists in the storage.                                                                                  | `bool`       |
| `ContainsAsync(string name)`                   | Asynchronously checks if a value with the specified name exists in the storage.                                                                   | `bool`       |
| `InsertValueAsync<T>(string name, T value)`    | Asynchronously adds or updates a value of the specified type `T` in the storage.                                                                  | `void`       |
| `InsertValue<T>(string name, T value)`         | Adds or updates a value of the specified type `T` in the storage.                                                                                 | `void`       |
| `GetValueAsync<T>(string name)`                | Asynchronously retrieves a value of the specified type `T` from the storage. Returns `null` if the value is not found or the type does not match. | `T?`         |
| `GetValue<T>(string name)`                     | Retrieves a value of the specified type `T` from the storage. Returns `null` if the value is not found or the type does not match.                | `T?`         |
| `RemoveValueAsync(string name)`                | Asynchronously removes a value with the specified name from the storage.                                                                          | `bool`       |
| `RemoveValue(string name)`                     | Removes a value with the specified name from the storage.                                                                                         | `bool`       |
| `InsertIntAsync(string name, int value)`       | Asynchronously adds or updates an integer value in the storage.                                                                                   | `void`       |
| `InsertInt(string name, int value)`            | Adds or updates an integer value in the storage.                                                                                                  | `void`       |
| `GetIntAsync(string name)`                     | Asynchronously retrieves an integer value from the storage. Returns `null` if the value is not found.                                             | `int?`       |
| `GetInt(string name)`                          | Retrieves an integer value from the storage. Returns `null` if the value is not found.                                                            | `int?`       |
| `InsertCharAsync(string name, char value)`     | Asynchronously adds or updates a character in the storage.                                                                                        | `void`       |
| `InsertChar(string name, char value)`          | Adds or updates a character in the storage.                                                                                                       | `void`       |
| `GetCharAsync(string name)`                    | Asynchronously retrieves a character from the storage. Returns `null` if the value is not found.                                                  | `char?`      |
| `GetChar(string name)`                         | Retrieves a character from the storage. Returns `null` if the value is not found.                                                                 | `char?`      |
| `InsertBoolAsync(string name, bool value)`     | Asynchronously adds or updates a boolean value in the storage.                                                                                    | `void`       |
| `InsertBool(string name, bool value)`          | Adds or updates a boolean value in the storage.                                                                                                   | `void`       |
| `GetBoolAsync(string name)`                    | Asynchronously retrieves a boolean value from the storage. Returns `null` if the value is not found.                                              | `bool?`      |
| `GetBool(string name)`                         | Retrieves a boolean value from the storage. Returns `null` if the value is not found.                                                             | `bool?`      |
| `InsertStringAsync(string name, string value)` | Asynchronously adds or updates a string value in the storage.                                                                                     | `void`       |
| `InsertString(string name, string value)`      | Adds or updates a string value in the storage.                                                                                                    | `void`       |
| `GetStringAsync(string name)`                  | Asynchronously retrieves a string value from the storage. Returns `null` if the value is not found.                                               | `string?`    |
| `GetString(string name)`                       | Retrieves a string value from the storage. Returns `null` if the value is not found.                                                              | `string?`    |
| `InsertDoubleAsync(string name, double value)` | Asynchronously adds or updates a double-precision floating-point value in the storage.                                                            | `void`       |
| `InsertDouble(string name, double value)`      | Adds or updates a double-precision floating-point value in the storage.                                                                           | `void`       |
| `GetDoubleAsync(string name)`                  | Asynchronously retrieves a double-precision floating-point value from the storage. Returns `null` if the value is not found.                      | `double?`    |
| `GetDouble(string name)`                       | Retrieves a double-precision floating-point value from the storage. Returns `null` if the value is not found.                                     | `double?`    |

**Notes:**

- All functions ending with `Async` are executed asynchronously, without blocking the main program thread. It is recommended to use asynchronous versions of functions to improve performance.
- The type `T` in the functions `InsertValueAsync<T>`, `InsertValue<T>`, `GetValueAsync<T>`, and `GetValue<T>` must be specified explicitly. The types `int`, `char`, `bool`, `string`, and `double` are supported. For other data types, use `InsertValueAsync` and `GetValueAsync` with explicit type specification.
- Before using any function, make sure that the storage service is initialized. This is usually done automatically by the program, but it's good to keep in mind.
- To get the result from asynchronous functions, you need to use `.Result`. For example: `local myVariable = Storage.GetStringAsync("myVariable").Result`

### Example usage in `config.lua`

```lua
local Storage = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Storage

-- Saving a string value
Storage.InsertStringAsync("myVariable", "Hello, World!")

-- Retrieving a string value
local myVariable = Storage.GetString("myVariable")
if myVariable then
  print("Value of myVariable: " .. myVariable)
else
  print("Variable myVariable not found")
end

-- Saving an integer value
Storage.InsertInt("myNumber", 42)

-- Retrieving an integer value
local myNumber = Storage.GetInt("myNumber")
if myNumber then
  print("Value of myNumber: " .. myNumber)
else
  print("Variable myNumber not found")
end

-- Checking if a value exists
local hasValue = Storage.Contains("myVariable")
print("Variable myVariable exists: " .. tostring(hasValue))

-- Removing a value
Storage.RemoveValueAsync("myVariable")

-- Checking if a value exists after removal
local hasValue = Storage.Contains("myVariable")
print("Variable myVariable exists after removal: " .. tostring(hasValue))

-- Asynchronously retrieving a string value
local myVariableAsync = Storage.GetStringAsync("myVariable").Result
if myVariableAsync then
    print("Asynchronous value of myVariable: " .. myVariableAsync)
else
    print("Asynchronous variable myVariable not found")
end
```
