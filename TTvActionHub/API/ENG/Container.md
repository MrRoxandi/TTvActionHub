--- START OF FILE Container.md ---

# Documentation for the 'Container' Module

This module provides an interface for a simple key-value data store within the application. It allows for saving, retrieving, checking for existence, and deleting data by a string key (name). Data is typically stored for the current application session, but after the service is stopped, the data will be saved to disk.

## Available Methods

### Basic Operations

| Method                     | Description                                                                                                                                   |
|----------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| `Contains(string name)`    | Checks if a record with the specified name (`name`) exists in the store. Returns `boolean` (`true` or `false`).                               |
| `RemoveValue(string name)` | Deletes the record with the specified name (`name`) from the store. Returns `boolean` (`true` if deletion was successful, otherwise `false`). |

### Methods for Basic Types (Recommended for Lua)

These methods provide a convenient way to work with basic Lua data types.

| Method                                    | Description                                                                                                                                          |
|:------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------|
| `InsertInt(string name, int value)`       | Saves or updates an integer (`number`) value `value` under the name `name`.                                                                          |
| `GetInt(string name)`                     | Retrieves an integer (`number`) value by the name `name`. Returns `number` or `nil` if the record is not found or has a different type.              |
| `InsertChar(string name, char value)`     | Saves or updates a character (`string` of length 1) value `value` under the name `name`.                                                             |
| `GetChar(string name)`                    | Retrieves a character (`string` of length 1) value by the name `name`. Returns `string` or `nil` if the record is not found or has a different type. |
| `InsertBool(string name, bool value)`     | Saves or updates a boolean (`boolean`) value `value` under the name `name`.                                                                          |
| `GetBool(string name)`                    | Retrieves a boolean (`boolean`) value by the name `name`. Returns `boolean` or `nil` if the record is not found or has a different type.             |
| `InsertString(string name, string value)` | Saves or updates a string (`string`) value `value` under the name `name`.                                                                            |
| `GetString(string name)`                  | Retrieves a string (`string`) value by the name `name`. Returns `string` or `nil` if the record is not found or has a different type.                |
| `InsertDouble(string name, double value)` | Saves or updates a floating-point number (`number`) value `value` under the name `name`.                                                             |
| `GetDouble(string name)`                  | Retrieves a floating-point number (`number`) value by the name `name`. Returns `number` or `nil` if the record is not found or has a different type. |

**Clarifications**:

- **Names (Keys):** Names (`name`) must be non-empty strings. Using incorrect names will result in an error.
- **Data Types:** When using `Get<Type>` methods (e.g., `GetInt`), if a value of a different type is stored under that name, the method will return `nil`.
- **`nil`:** A returned value of `nil` from `Get<Type>` or `GetValue` methods means either the key was not found, or the stored value has an incompatible type (in the case of `Get<Type>`).

## Example Usage

```lua
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
        print('Failed to retrieve score (possibly a different data type was saved).')
    end
else
    print('Score record not found.')
end

-- Get username
local user = Container.GetString('username')
print('Username: ' .. (user or 'Not set')) -- Will print 'CoolUser123' or 'Not set' if the key is not found

-- Remove activation flag
local wasRemoved = Container.RemoveValue('isEnabled')
if wasRemoved then
    print('isEnabled flag removed.')
else
    print('Failed to remove isEnabled flag (it might have already been gone).')
end

-- Attempting to get a removed value will return nil
local enabledStatus = Container.GetBool('isEnabled')
if enabledStatus == nil then
    print('isEnabled status is now nil (not found).')
end
```