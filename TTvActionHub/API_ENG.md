## Documentation for Scripts in Configuration

This document describes how to write `LUA` code to utilize the capabilities of the program on your Twitch channel.

## Available Modules in `TTvActionHub.LuaTools`

Any module is connected through `TTvActionHub.LuaTools.<module>`. Below is a table of all possible connections, followed by an example of connecting the audio module.

| Module                                    | Description                                                                | Documentation                       |
| ----------------------------------------- | -------------------------------------------------------------------------- | ----------------------------------- |
| `TTvActionHub.LuaTools.Audio.Sounds`      | This module is responsible for playing music                               | [Documentation](API/EN/Sounds.md)   |
| `TTvActionHub.LuaTools.Hardware.Keyboard` | This module is responsible for keyboard emulation                          | [Documentation](API/EN/Keyboard.md) |
| `TTvActionHub.LuaTools.Hardware.Mouse`    | This module is responsible for mouse emulation                             | [Documentation](API/EN/Mouse.md)    |
| `TTvActionHub.LuaTools.Stuff.Chat`        | This module is responsible for interacting with the Twitch chat            | [Documentation](API/EN/Chat.md)     |
| `TTvActionHub.LuaTools.Stuff.Storage`     | This module is responsible for interacting with a special internal storage | [Documentation](API/EN/Storage.md)  |
| `TTvActionHub.LuaTools.Stuff.Funcs`       | This module contains useful functions for writing configuration            | [Documentation](API/EN/Funcs.md)    |

### Example of Connecting the Music Playback Module

```lua
local Audio = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds
```

## Configuration File Requirements

- File format: the configuration must be a file named `config.lua`.
- Location: the configuration must be located in the root directory of the program.
- Return value: the configuration **must** return a lua table containing configuration [information](#configuration-table).
- Encoding: use UTF-8 encoding for your scripts. If anything is unclear, simply run the program without a configuration file; it will be generated in the root directory.

## Configuration Table

The configuration table contains several parameters:

| Parameter       | Type        | Values     | Description                                                                                                        | Optional | Default Value |
| --------------- | ----------- | ---------- | ------------------------------------------------------------------------------------------------------------------ | -------- | ------------- |
| force-relog     | `bool`      | true/false | If `true`, authorization will be forcibly requested through the browser without attempting to refresh information. | +        | `false`       |
| timeout         | `int`       | Number     | Default recovery time for chat commands if they do not specify their own.                                          | +        | `30000`       |
| logs            | `bool`      | true/false | If `true`, internal logs of Twitch-related services will be outputted                                              | +        | `false`       |
| opening-bracket | `char`      | Symbol     | The symbol that will be considered the start of argument transmission from the chat participant.                   | +        | `null`        |
| closing-bracket | `char`      | Symbol     | The symbol that will be considered the end of argument transmission from the chat participant.                     | +        | `null`        |
| rewards         | `lua-table` | Table      | A table containing definitions of custom [commands](#commands).                                                    | +        | `null`        |
| commands        | `lua-table` | Table      | A table containing definitions of custom [rewards](#rewards)                                                       | +        | `null`        |

## Commands

The Commands table contains definitions of your custom commands. Each key in this table represents the name of the command (without the prefix "!"). The value associated with each command name is another table containing the following keys:

| Key     | Type     | Description                                                                                                    | Optional |
| ------- | -------- | -------------------------------------------------------------------------------------------------------------- | -------- |
| action  | Function | The function that will be executed when the command is triggered. It takes two arguments: `sender` and `args`. | -        |
| timeout | int      | Recovery time for the command. Measured in ms. Three possible values: -1, 0, > 0                               | +        |

Since the function that the `action` field takes is not that simple, here is a bit more detail about it:

| Argument | Type     | Description                                                                                                  |
| -------- | -------- | ------------------------------------------------------------------------------------------------------------ |
| sender   | string   | A string containing the Twitch username of the user who sent this command.                                   |
| args     | string[] | An array of strings containing all the words that the user wrote when sending this command, split by spaces. |

Example of creating a command:

```lua
local commands = {}
-- Some other useful code...
local cmd1
```
