## Scripting Documentation in Configuration

This document describes how to write `LUA` code to leverage the program's capabilities on your Twitch channel.

## Available Modules in `TTvActionHub.LuaTools`

Any module is accessed via `TTvActionHub.LuaTools.<module>`. The table below lists all possible connections, followed by an example of connecting the sound module.

| Module                                    | Description                                                                | Documentation                        |
| ----------------------------------------- | -------------------------------------------------------------------------- | ------------------------------------ |
| `TTvActionHub.LuaTools.Audio.Sounds`      | This module is responsible for playing music                               | [Documentation](API/ENG/Sounds.md)   |
| `TTvActionHub.LuaTools.Hardware.Keyboard` | This module is responsible for keyboard emulation                          | [Documentation](API/ENG/Keyboard.md) |
| `TTvActionHub.LuaTools.Hardware.Mouse`    | This module is responsible for mouse emulation                             | [Documentation](API/ENG/Mouse.md)    |
| `TTvActionHub.LuaTools.Stuff.TwitchChat`  | This module is responsible for interacting with the Twitch chat            | [Documentation](API/ENG/Chat.md)     |
| `TTvActionHub.LuaTools.Stuff.Storage`     | This module is responsible for interacting with a special internal storage | [Documentation](API/ENG/Storage.md)  |
| `TTvActionHub.LuaTools.Stuff.Funcs`       | This module contains useful functions for writing configurations           | [Documentation](API/ENG/Funcs.md)    |
| `TTvActionHub.LuaTools.Stuff.Users`       | This module contains access levels used for [commands](#commands)          | ~~[Documentation]~~                  |

### Example of connecting the music playback module.

```lua
local Audio = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds
```

## Configuration File Requirements

- File Format: The configuration must be a `config.lua` file.
- Location: The configuration must be located in the program's root directory.
- Return Value: The configuration **must** return a Lua table containing configuration [information](#configuration-table).
- Encoding: Use UTF-8 encoding for your scripts.
  If anything is unclear, simply run the program without a configuration file; it will be generated in the root directory.

## Configuration Table (file ../configs/config.lua) [Example](example/config.md)

The configuration table contains several parameters:

| Parameter       | Type   | Values     | Description                                                                                        | Optional | Default Value |
| --------------- | ------ | ---------- | -------------------------------------------------------------------------------------------------- | -------- | ------------- |
| force-relog     | `bool` | true/false | If `true`, authorization via browser will be forced without attempting to refresh information.     | +        | `false`       |
| timeout         | `int`  | Number     | Standard cooldown time for chat commands if they don't have their own specified.                   | +        | `30000`       |
| logs            | `bool` | true/false | If `true`, internal service logs related to Twitch will be outputted                               | +        | `false`       |
| opening-bracket | `char` | Symbol     | The symbol that will be considered the beginning of argument transmission from a chat participant. | +        | `null`        |
| closing-bracket | `char` | Symbol     | The symbol that will be considered the end of argument transmission from a chat participant.       | +        | `null`        |

## Commands (file ../configs/commands.lua) [Example](example/commands.md)

The Commands table contains definitions for your custom commands. Each key in this table represents a command name (without the "!" prefix). The value associated with each command name is another table containing the following keys:

| Key     | Type      | Description                                                                                                 | Optional | Default Value                                        |
| ------- | --------- | ----------------------------------------------------------------------------------------------------------- | -------- | ---------------------------------------------------- |
| action  | Function  | The function that will be executed when the command is triggered. Takes two arguments: `sender` and `args`. | -        | -                                                    |
| timeout | int       | Cooldown time for the command. Measured in ms. Three possible values: -1, 0, > 0                            | +        | Value from the [configuration](#configuration-table) |
| perm    | enum(int) | Access level to the commands. Defined by an integer from 0 to 4. (default value 0)                          | +        | 0 (VIEWIER)                                          |

Since `perm` is primarily an enum, it has uppercase values that can be used to understand which access level you are assigning to the command:

```cs
enum USERLEVEL: int
    {
        VIEWIER = 0, VIP = 1, SUB = 2, MODERATOR = 3, BROADCASTER = 4
    }
```

Since the function accepted by the `action` field is not so simple, here's a little more detail about it:

| Argument | Type     | Description                                                                                             |
| -------- | -------- | ------------------------------------------------------------------------------------------------------- |
| sender   | string   | A string containing the Twitch username of the person who sent the command.                             |
| args     | string[] | An array of strings containing all the words written by the user who sent the command, split by spaces. |

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

## Rewards (File ../configs/rewards.lua) [Example](example/rewards.md)

The Rewards table contains definitions for your custom rewards. Each key in this table represents the name of the reward (which you specified in the Twitch panel). The value associated with each reward name is another table containing the following keys:

| Key    | Type     | Description                                                                                                | Optional |
| ------ | -------- | ---------------------------------------------------------------------------------------------------------- | -------- |
| action | Function | The function that will be executed when the reward is activated. Takes two arguments: `sender` and `args`. | -        |

Since the function accepted by the `action` field is not so simple, here's a little more detail about it:

| Argument | Type     | Description                                                                                                 |
| -------- | -------- | ----------------------------------------------------------------------------------------------------------- |
| sender   | string   | A string containing the Twitch username of the person who activated the reward.                             |
| args     | string[] | An array of strings containing all the words written by the user who activated the reward, split by spaces. |

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

## Timed Events (File ../configs/timeractions.lua) [Example](example/timeractions.md)

The `tactions` table contains definitions for your custom timed events. Each key in this table represents the name of the event. The value associated with each event name is another table containing the following keys:

| Key     | Type     | Description                                                                               | Optional |
| ------- | -------- | ----------------------------------------------------------------------------------------- | -------- |
| action  | Function | The function that will be executed when the timed event is triggered. Takes no arguments. | -        |
| timeout | integer  | Time interval after which the event will be triggered. Any values >= 1 are valid.         | -        |

Since the function accepted by the `action` field is simple, it does not take any arguments.

Example of creating a timed event:

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
