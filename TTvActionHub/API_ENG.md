## Scripting Documentation in Configuration

This document describes how to write `LUA` code to leverage the program's features on your Twitch channel.

## Available Modules in `TTvActionHub.LuaTools`

Any module is accessed via `TTvActionHub.LuaTools.<module>`. A table of all possible connections is provided below, followed by an example of connecting the sound module.

| Module                                    | Description                                                                | Documentation                        |
| ----------------------------------------- | -------------------------------------------------------------------------- | ------------------------------------ |
| `TTvActionHub.LuaTools.Audio.Sounds`      | This module is responsible for playing music                               | [Documentation](API/ENG/Sounds.md)   |
| `TTvActionHub.LuaTools.Hardware.Keyboard` | This module is responsible for keyboard emulation                          | [Documentation](API/ENG/Keyboard.md) |
| `TTvActionHub.LuaTools.Hardware.Mouse`    | This module is responsible for mouse emulation                             | [Documentation](API/ENG/Mouse.md)    |
| `TTvActionHub.LuaTools.Stuff.Chat`        | This module is responsible for interacting with the Twitch chat            | [Documentation](API/ENG/Chat.md)     |
| `TTvActionHub.LuaTools.Stuff.Storage`     | This module is responsible for interacting with a special internal storage | [Documentation](API/ENG/Storage.md)  |
| `TTvActionHub.LuaTools.Stuff.Funcs`       | This module contains useful functions for writing the configuration        | [Documentation](API/ENG/Funcs.md)    |

### Example of Connecting the Music Playback Module.

```lua
local Audio = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds
```

## Configuration File Requirements

- File format: The configuration must be a `config.lua` file.
- Location: The configuration must be located in the program's root directory.
- Return value: The configuration **must** return a Lua table containing configuration [information](#configuration-table).
- Encoding: Use UTF-8 encoding for your scripts.
  If anything is unclear, simply run the program without a configuration file, and one will be generated in the root directory.

## Configuration Table

The configuration table contains several parameters:

| Parameter       | Type        | Values     | Description                                                                                                    | Optional | Default Value |
| --------------- | ----------- | ---------- | -------------------------------------------------------------------------------------------------------------- | -------- | ------------- |
| force-relog     | `bool`      | true/false | If `true`, authorization will be forcibly requested via the browser without attempting to refresh information. | +        | `false`       |
| timeout         | `int`       | Number     | The default cooldown time for chat commands if they don't have their own specified.                            | +        | `30000`       |
| logs            | `bool`      | true/false | If `true`, internal logs of Twitch-related services will be output.                                            | +        | `false`       |
| opening-bracket | `char`      | Character  | The character that will be considered the beginning of the argument transmission from a chat participant.      | +        | `null`        |
| closing-bracket | `char`      | Character  | The character that will be considered the end of the argument transmission from a chat participant.            | +        | `null`        |
| rewards         | `lua-table` | Table      | A table containing the definitions of custom [rewards](#rewards).                                              | +        | `null`        |
| commands        | `lua-table` | Table      | A table containing the definitions of custom [commands](#commands).                                            | +        | `null`        |
| tevents         | `lua-table` | Table      | A table containing the definitions of custom [timed events](#timed-events), triggered on a timer.              | +        | `null`        |

## Commands

The `Commands` table contains the definitions of your custom commands. Each key in this table represents the command name (without the "!" prefix). The value associated with each command name is another table containing the following keys:

| Key     | Type     | Description                                                                                                 | Optional |
| ------- | -------- | ----------------------------------------------------------------------------------------------------------- | -------- |
| action  | Function | The function that will be executed when the command is triggered. Takes two arguments: `sender` and `args`. | -        |
| timeout | int      | The cooldown time for the command, measured in milliseconds. Three possible values: -1, 0, > 0              | +        |

Since the function accepted by the `action` field is not so simple, here is a little more detail about it:

| Argument | Type     | Description                                                                                                 |
| -------- | -------- | ----------------------------------------------------------------------------------------------------------- |
| sender   | string   | A string containing the Twitch username of the user who sent the command.                                   |
| args     | string[] | An array of strings containing all the words written by the user when sending the command, split by spaces. |

Example of creating a command:

```lua
local commands = {}
-- Some other usefull code...
local cmd1 = {}
cmd1["action"] =
    function(sender, args)
        -- Some usefull work here with sender and args
    end
cmd1["timeout"] = 1000 -- 1 second cooldown

commands["testcmd"] = cmd1
```

## Rewards

The `Rewards` table contains the definitions of your custom rewards. Each key in this table represents the reward name (which you specified in the Twitch panel). The value associated with each reward name is another table containing the following keys:

| Key    | Type     | Description                                                                                                | Optional |
| ------ | -------- | ---------------------------------------------------------------------------------------------------------- | -------- |
| action | Function | The function that will be executed when the reward is activated. Takes two arguments: `sender` and `args`. | -        |

Since the function accepted by the `action` field is not so simple, here is a little more detail about it:

| Argument | Type     | Description                                                                                                  |
| -------- | -------- | ------------------------------------------------------------------------------------------------------------ |
| sender   | string   | A string containing the Twitch username of the user who redeemed the reward.                                 |
| args     | string[] | An array of strings containing all the words written by the user when redeeming the reward, split by spaces. |

Example of creating a reward:

```lua
local rewards = {}
-- Some other usefull code...
local reward1 = {}
reward1["action"] =
    function(sender, args)
        -- Some usefull work here with sender and args
    end

rewards["testreward"] = reward1
```

## Timed Events

The `tevents` table contains the definitions of your custom timed events. Each key in this table represents the event name. The value associated with each event name is another table containing the following keys:

| Key     | Type     | Description                                                                         | Optional |
| ------- | -------- | ----------------------------------------------------------------------------------- | -------- |
| action  | Function | The function that will be executed when the event is triggered. Takes no arguments. | -        |
| timeout | integer  | The time interval after which the event will be called. Any value >= 1 is allowed.  | -        |

Since the function accepted by the `action` field is simple, the function takes no arguments.

Example of creating a timed event:

```lua
local tevents = {}
-- Some other usefull code...
local tevent1 = {}
tevent1["action"] =
    function()
        Chat.SendMessageAsync("Test")
    end
tevent1["timeout"] = 5000
tevents["test"] = tevent1
```
