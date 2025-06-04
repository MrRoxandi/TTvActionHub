# Documentation for Scripts in Configuration

This document describes how to write `LUA` "code" to use the program's features on your Twitch channel.

## Available Modules in `TTvActionHub`

| Module        | Description                                                                                                    | Documentation                           |
|---------------|----------------------------------------------------------------------------------------------------------------|-----------------------------------------|
| `Keyboard`    | This module is responsible for keyboard emulation                                                              | [Documentation](API/ENG/Keyboard.md)    |
| `Mouse`       | This module is responsible for mouse emulation                                                                 | [Documentation](API/ENG/Mouse.md)       |
| `Audio`       | This module is responsible for interacting with the sound service (playing audio files) and for music playback | [Documentation](API/ENG/Audio.md)       |
| `Container`   | This module is responsible for interacting with a special internal storage                                     | [Documentation](API/ENG/Container.md)   |
| `TwitchTools` | This module is responsible for interacting with Twitch                                                         | [Documentation](API/ENG/TwitchTools.md) |
| `Funcs`       | This module contains useful functions for writing configurations                                               | [Documentation](API/ENG/Funcs.md)       |

## Configuration File Requirements

- File format: The configuration must be a `lua` file.
- Location: The configuration must be located in the `..\configs\` directory.
- Return value: The configuration **must** return a lua table containing configuration information.
- Encoding: Use UTF-8 encoding for your scripts.
  If any of this is unclear, simply run the program without configuration files, and they will be generated automatically.

## Main Configuration Table (file ../configs/Config.lua) [Example](example/Config.md)

The configuration table contains several parameters:

| Parameter   | Type   | Values     | Description                                                                                                     | Optional | Default value |
|-------------|--------|------------|-----------------------------------------------------------------------------------------------------------------|----------|---------------|
| force-relog | `bool` | true/false | If `true`, authorization through the browser will be forcibly requested without attempts to update information. | +        | `false`       |
| timeout     | `int`  | Number     | Default cooldown time for chat commands if they don't have their own specified.                                 | +        | `30000`       |
| logs        | `bool` | true/false | If `true`, internal logs of services related to Twitch will be output.                                          | +        | `false`       |

## Events (file ../configs/TwitchEvents.lua) [Example](example/TwitchEvents.md)

The TwitchEvents table contains definitions for your custom events. Each key in this table represents an event name. The value associated with each event name is another table containing the following keys:

| Key     | Type       | Description                                                                                                | Optional | Default value           |
|---------|------------|------------------------------------------------------------------------------------------------------------|----------|-------------------------|
| kind    | Event Type | Defines the event type: Command, channel point reward, or other.                                           | -        | -                       |
| action  | Function   | Function that will be executed when the command is triggered. It takes two arguments: `sender` and `args`. | -        | -                       |
| timeout | int        | Cooldown time for the command. Measured in ms. Three possible values: -1, 0, > 0                           | +        | timeout from config.lua |
| perm    | enum(int)  | Access level for the event.                                                                                | +        | Viewer                  |
| cost    | long       | Defines the cost to trigger the event.                                                                     | +        | 0                       |

The `kind` parameter is `TwitchEventKind` and can take only two values. The structure of the `kind` parameter is shown below.

```cs
public enum TwitchEventKind : byte
{
    Command = 0, TwitchReward
}
```

Below is an example of usage.

```lua
-- ...
twitchevents['test'] = {}
twitchevents['test']['kind'] = TwitchTools.TwitchEventKind("Command") -- TwitchTools.TwitchEventKind.TwitchReward
-- ...
```

The `action` parameter is a `lua-function`. An example of writing and using other modules is shown below.

```lua
-- ...
twitchevents['test'] = {}
twitchevents['test']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@' .. sender .. ' -> test')
  end
-- ...
```

The parameters `sender` and `args` accepted by the function:

- `sender` - a string containing the Twitch username of the user who sent this command.
- `args` - an array of strings, containing all the words written by the user who sent this command, split by spaces.

The `perm` parameter is `PermissionLevel` and can take only five values. The structure of the `perm` parameter is shown below.

```cs
public enum PermissionLevel : int
{
    Viewer, Vip, Subscriber, Moderator, Broadcaster
}
```

Below is an example of usage.

```lua
-- ...
twitchevents['test'] = {}
twitchevents['test']['perm'] = TwitchTools.PermissionLevel("Vip") -- TwitchTools.PermissionLevel.Viewer
-- ...
```

## Timer Actions (File ../configs/TimerActions.lua) [Example](example/TimerActions.md)

The tactions table contains definitions for your custom events. Each key in this table represents an event name (which you specified in the Twitch panel). The value associated with each event name is another table containing the following keys:

| Key     | Type     | Description                                                                                  | Optional |
|---------|----------|----------------------------------------------------------------------------------------------|----------|
| action  | Function | Function that will be executed when the reward is activated. It does not take any arguments. | -        |
| timeout | integer  | Time interval after which this event will be triggered. Any values >= 1 are available.       | -        |

Since the function that the `action` field accepts is simple, the function does not accept any arguments.

Example of creating a reward:

```lua
local tactions = {}
-- Some other useful code...
local taction1 = {}
taction1["action"] =
  function(sender, args)
    TwitchTools.SendMessage('@' .. sender .. ' -> test')
  end
taction1["timeout"] = 5000
tactions["test"] = taction1
```

--- END OF FILE API_EN.md ---