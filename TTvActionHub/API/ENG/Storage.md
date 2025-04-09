## Documentation for the 'Storage' module in `TTvActionHub.LuaTools.Stuff`

This module provides an interface for a simple key-value data storage within the application. It allows saving, retrieving, checking for existence, and deleting data using a string key (name). Data is typically stored for the duration of the current application session, but it will be saved to disk when the service is stopped.

### Connecting in the configuration file

Module connection example:

```lua
local Storage = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Storage
```

### Usage Example

```lua
local Storage = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Storage

-- Save username (string)
Storage.InsertString('username', 'CoolUser123')

-- Save score (integer)
Storage.InsertInt('score', 1500)

-- Save activation flag (boolean)
Storage.InsertBool('isEnabled', true)

-- Check if the 'score' value exists
if Storage.Contains('score') then
    -- Get the 'score' value
    local currentScore = Storage.GetInt('score')
    if currentScore then -- Make sure the value was retrieved (not nil)
        print('Current score: ' .. currentScore)
        -- Increase the score and save it back
        Storage.InsertInt('score', currentScore + 100)
    else
        print('Failed to retrieve score (perhaps a different data type was saved).')
    end
else
    print('Score record not found.')
end

-- Get the username
local user = Storage.GetString('username')
print('Username: ' .. (user or 'Not set')) -- Will print 'CoolUser123' or 'Not set' if the key is not found

-- Remove the activation flag
local wasRemoved = Storage.RemoveValue('isEnabled')
if wasRemoved then
    print('isEnabled flag removed.')
else
    print('Failed to remove isEnabled flag (perhaps it was already gone).')
end

-- Attempting to get the removed value will return nil
local enabledStatus = Storage.GetBool('isEnabled')
if enabledStatus == nil then
    print('isEnabled status is now nil (not found).')
end
```

### Available methods

#### Basic Operations

| Method                     | Description                                                                                                                       |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `Contains(string name)`    | Checks if an entry with the specified name (`name`) exists in the storage. Returns `boolean` (`true` or `false`).                 |
| `RemoveValue(string name)` | Removes the entry with the specified name (`name`) from the storage. Returns `boolean` (`true` if successful, `false` otherwise). |

#### Methods for Basic Types (Recommended for Lua)

These methods provide a convenient way to work with basic Lua data types.

| Method                                    | Description                                                                                                                                     |
| :---------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------- |
| `InsertInt(string name, number value)`    | Saves or updates an integer (`number`) value `value` under the name `name`.                                                                     |
| `GetInt(string name)`                     | Retrieves an integer (`number`) value by name `name`. Returns `number` or `nil` if the entry is not found or has a different type.              |
| `InsertChar(string name, string value)`   | Saves or updates a character (`string` of length 1) value `value` under the name `name`.                                                        |
| `GetChar(string name)`                    | Retrieves a character (`string` of length 1) value by name `name`. Returns `string` or `nil` if the entry is not found or has a different type. |
| `InsertBool(string name, boolean value)`  | Saves or updates a boolean (`boolean`) value `value` under the name `name`.                                                                     |
| `GetBool(string name)`                    | Retrieves a boolean (`boolean`) value by name `name`. Returns `boolean` or `nil` if the entry is not found or has a different type.             |
| `InsertString(string name, string value)` | Saves or updates a string (`string`) value `value` under the name `name`.                                                                       |
| `GetString(string name)`                  | Retrieves a string (`string`) value by name `name`. Returns `string` or `nil` if the entry is not found or has a different type.                |
| `InsertDouble(string name, number value)` | Saves or updates a floating-point number (`number`) value `value` under the name `name`.                                                        |
| `GetDouble(string name)`                  | Retrieves a floating-point number (`number`) value by name `name`. Returns `number` or `nil` if the entry is not found or has a different type. |

**Clarifications**:

- **Names (Keys):** Names (`name`) must be non-empty strings. Using invalid names will result in an error.
- **Data Types:** When using `Get<Type>` methods (e.g., `GetInt`), if the value stored under that name is of a different type, the method will return `nil`.
- **`nil`:** A `nil` value returned from `Get<Type>` methods means either the key was not found or the stored value has an incompatible type (in the case of `Get<Type>`).
