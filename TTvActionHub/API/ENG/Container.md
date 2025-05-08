# Documentation for the 'Container' module in `TTvActionHub.LuaTools.Services.Container`

This module provides an interface for a simple key-value data store within the application. It allows you to save, retrieve, check for the existence of, and delete data using a string key (name). Data is typically stored for the current application session, but after the service is stopped, the data will be saved to disk.

## Importing into the configuration file

Example of importing the module:

```lua
local Container = import('TTvActionHub', 'TTvActionHub.LuaTools.Services').Container
```

## Available Methods

### Basic Operations

| Method                     | Description                                                                                                                     |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Contains(string name)`    | Checks if an entry with the specified name (`name`) exists in the store. Returns `boolean` (`true` or `false`).                 |
| `RemoveValue(string name)` | Removes the entry with the specified name (`name`) from the store. Returns `boolean` (`true` if successful, `false` otherwise). |

### Methods for Basic Types (Recommended for Lua)

These methods provide a convenient way to work with basic Lua data types.

| Method                                    | Description                                                                                                                                     |
| :---------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------- |
| `InsertInt(string name, int value)`       | Saves or updates an integer (`number`) value `value` under the name `name`.                                                                     |
| `GetInt(string name)`                     | Retrieves an integer (`number`) value by name `name`. Returns `number` or `nil` if the entry is not found or has a different type.              |
| `InsertChar(string name, char value)`     | Saves or updates a character (`string` of length 1) value `value` under the name `name`.                                                        |
| `GetChar(string name)`                    | Retrieves a character (`string` of length 1) value by name `name`. Returns `string` or `nil` if the entry is not found or has a different type. |
| `InsertBool(string name, bool value)`     | Saves or updates a boolean (`boolean`) value `value` under the name `name`.                                                                     |
| `GetBool(string name)`                    | Retrieves a boolean (`boolean`) value by name `name`. Returns `boolean` or `nil` if the entry is not found or has a different type.             |
| `InsertString(string name, string value)` | Saves or updates a string (`string`) value `value` under the name `name`.                                                                       |
| `GetString(string name)`                  | Retrieves a string (`string`) value by name `name`. Returns `string` or `nil` if the entry is not found or has a different type.                |
| `InsertDouble(string name, double value)` | Saves or updates a floating-point number (`number`) value `value` under the name `name`.                                                        |
| `GetDouble(string name)`                  | Retrieves a floating-point number (`number`) value by name `name`. Returns `number` or `nil` if the entry is not found or has a different type. |

**Clarifications**:

- **Names (Keys):** Names (`name`) must be non-empty strings. Using incorrect names will result in an error.
- **Data Types:** When using `Get<Type>` methods (e.g., `GetInt`), if the store contains a value of a different type under that name, the method will return `nil`.
- **`nil`:** A returned `nil` value from `Get<Type>` methods or `GetValue` means that either the key was not found, or the stored value has an incompatible type (in the case of `Get<Type>`).

## Usage Example

```lua
local Container = import('TTvActionHub', 'TTvActionHub.LuaTools.Services').Container

-- Save username (string)
Container.InsertString('username', 'CoolUser123')

-- Save score (integer)
Container.InsertInt('score', 1500)

-- Save activation flag (boolean)
Container.InsertBool('isEnabled', true)

-- Check if 'score' value exists
if Container.Contains('score') then
    -- Get 'score' value
    local currentScore = Container.GetInt('score')
    if currentScore then -- Make sure the value was retrieved (not nil)
        print('Current score: ' .. currentScore)
        -- Increase score and save it back
        Container.InsertInt('score', currentScore + 100)
    else
        print('Failed to get score (perhaps a different data type is stored).')
    end
else
    print('Score record not found.')
end

-- Get username
local user = Container.GetString('username')
print('Username: ' .. (user or 'Not set')) -- Will output 'CoolUser123' or 'Not set' if the key is not found

-- Remove activation flag
local wasRemoved = Container.RemoveValue('isEnabled')
if wasRemoved then
    print('isEnabled flag removed.')
else
    print('Failed to remove isEnabled flag (perhaps it was already gone).')
end

-- Attempting to get a removed value will return nil
local enabledStatus = Container.GetBool('isEnabled')
if enabledStatus == nil then
    print('isEnabled status is now nil (not found).')
end
```
