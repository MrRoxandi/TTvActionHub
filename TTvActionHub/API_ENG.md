## Scripting Documentation in Configuration

This document describes how to write `LUA` code to utilize the program's features on your Twitch channel.

## Available Modules in `TTvActionHub.LuaTools`

Any module is accessed via `TTvActionHub.LuaTools.<module>`. Below is a table of all possible connections, followed by an example of connecting the sound module.

| Module                                    | Description                                                                | Documentation                        |
| ----------------------------------------- | -------------------------------------------------------------------------- | ------------------------------------ |
| `TTvActionHub.LuaTools.Audio.Sounds`      | This module is responsible for playing music                               | [Documentation](API/ENG/Sounds.md)   |
| `TTvActionHub.LuaTools.Hardware.Keyboard` | This module is responsible for keyboard emulation                          | [Documentation](API/ENG/Keyboard.md) |
| `TTvActionHub.LuaTools.Hardware.Mouse`    | This module is responsible for mouse emulation                             | [Documentation](API/ENG/Mouse.md)    |
| `TTvActionHub.LuaTools.Stuff.Chat`        | This module is responsible for interacting with the Twitch chat            | [Documentation](API/ENG/Chat.md)     |
| `TTvActionHub.LuaTools.Stuff.Storage`     | This module is responsible for interacting with a special internal storage | [Documentation](API/ENG/Storage.md)  |
| `TTvActionHub.LuaTools.Stuff.Funcs`       | This module contains useful functions for writing the configuration        | [Documentation](API/ENG/Funcs.md)    |
| `TTvActionHub.LuaTools.Stuff.Users`       | This module contains access levels that are used for [commands](#commands) | ~~[Documentation]~~                  |

### Example of connecting the music playback module.

```lua
local Audio = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds
```

## Configuration File Requirements

- File format: the configuration must be a `config.lua` file.
- Location: the configuration must be located in the program's root directory.
- Return value: the configuration **must** return a Lua table containing configuration [information](#configuration-table).
- Encoding: use UTF-8 encoding for your scripts.
  If anything is unclear, just run the program without a configuration file, and it will be generated in the root directory.

## Configuration Table

The configuration table contains several parameters:

| Parameter       | Type        | Values     | Description                                                                                                    | Optional | Default Value |
| --------------- | ----------- | ---------- | -------------------------------------------------------------------------------------------------------------- | -------- | ------------- |
| force-relog     | `bool`      | true/false | If `true`, authorization via the browser will be forcibly requested without attempting to refresh information. | +        | `false`       |
| timeout         | `int`       | Number     | Standard cooldown time for chat commands, if they do not have their own specified.                             | +        | `30000`       |
| logs            | `bool`      | true/false | If `true`, internal logs of the services related to Twitch will be output.                                     | +        | `false`       |
| opening-bracket | `char`      | Character  | Character that will be considered the beginning of the argument transmission from the chat participant.        | +        | `null`        |
| closing-bracket | `char`      | Character  | Character that will be considered the end of the argument transmission from the chat participant.              | +        | `null`        |
| rewards         | `lua-table` | Table      | Table containing the definitions of custom [commands](#commands).                                              | +        | `null`        |
| commands        | `lua-table` | Table      | Table containing the definitions of custom [rewards](#rewards).                                                | +        | `null`        |
| tactions        | `lua-table` | Table      | Table containing the definitions of custom [timer events](#timer-events) that trigger on a timer.              | +        | `null`        |

## Commands

The Commands table contains the definitions of your custom commands. Each key in this table represents the name of the command (without the "!" prefix). The value associated with each command name is another table containing the following keys:

| Key     | Type      | Description                                                                                             | Optional | Default Value                                        |
| ------- | --------- | ------------------------------------------------------------------------------------------------------- | -------- | ---------------------------------------------------- |
| action  | Function  | Function that will be executed when the command is triggered. Takes two arguments: `sender` and `args`. | -        | -                                                    |
| timeout | int       | Cooldown time for the command. Measured in ms. Three possible values: -1, 0, > 0                        | +        | Value from the [configuration](#configuration-table) |
| perm    | enum(int) | Access level to the commands. Defined by an integer from 0 to 4. (default value is 0)                   | +        | 0 (VIEWIER)                                          |

Because `perm` is primarily an enum, it has uppercase values that can be used to understand what access level you are granting to the command:

```cs
enum USERLEVEL: int
    {
        VIEWIER = 0, VIP = 1, SUB = 2, MODERATOR = 3, BROADCASTER = 4
    }
```

Since the function accepted by the `action` field is not that simple, here's a bit more detail about it:

| Argument | Type     | Description                                                                                               |
| -------- | -------- | --------------------------------------------------------------------------------------------------------- |
| sender   | string   | String containing the Twitch username of the user who sent this command.                                  |
| args     | string[] | Array of strings containing all the words that the user wrote when sending this command, split by spaces. |

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
cmd1["perm"] = Users.USERLEVEL.VIEWIER
commands["testcmd"] = cmd1
```

## Rewards

The Rewards table contains the definitions of your custom rewards. Each key in this table represents the name of the reward (which you specified in the Twitch panel). The value associated with each reward name is another table containing the following keys:

| Key    | Type     | Description                                                                                            | Optional |
| ------ | -------- | ------------------------------------------------------------------------------------------------------ | -------- |
| action | Function | Function that will be executed when the reward is activated. Takes two arguments: `sender` and `args`. | -        |

Since the function accepted by the `action` field is not that simple, here's a bit more detail about it:

| Argument | Type     | Description                                                                                               |
| -------- | -------- | --------------------------------------------------------------------------------------------------------- |
| sender   | string   | String containing the Twitch username of the user who sent this command.                                  |
| args     | string[] | Array of strings containing all the words that the user wrote when sending this command, split by spaces. |

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

## Timer Events

The tactions table contains the definitions of your custom events. Each key in this table represents the name of the event (which you specified in the Twitch panel). The value associated with each event name is another table containing the following keys:

| Key     | Type     | Description                                                                                        | Optional |
| ------- | -------- | -------------------------------------------------------------------------------------------------- | -------- |
| action  | Function | Function that will be executed when the reward is activated. Does not have any accepted arguments. | -        |
| timeout | integer  | Time interval after which this event will be called. Any values >= 1 are allowed.                  | -        |

Since the function accepted by the `action` field is simple, the function doesn't accept any arguments.

Example of creating a reward:

```lua
local tactions = {}
-- Some other usefull code...
local taction1 = {}
taction1["action"] =
    function()
        Chat.SendMessageAsync("Test")
    end
taction1["timeout"] = 5000
tactions["test"] = taction1
```
