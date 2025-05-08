# Configuration Scripting Documentation

This document describes how to write `LUA` "code" to use the program's features on your Twitch channel.

## Available Modules in `TTvActionHub.LuaTools`

Any module is imported via `TTvActionHub.LuaTools.<module_name>`. The table below lists all available modules, and following the table is an example of importing the audio module.

| Module                                       | Description                                                                                   | Documentation                          |
| -------------------------------------------- | --------------------------------------------------------------------------------------------- | -------------------------------------- |
| `TTvActionHub.LuaTools.Hardware.Keyboard`    | This module is responsible for keyboard emulation.                                            | [Documentation](API/EN/Keyboard.md)    |
| `TTvActionHub.LuaTools.Hardware.Mouse`       | This module is responsible for mouse emulation.                                               | [Documentation](API/EN/Mouse.md)       |
| `TTvActionHub.LuaTools.Services.Audio`       | This module handles interaction with the audio service (playing audio files, music playback). | [Documentation](API/EN/Audio.md)       |
| `TTvActionHub.LuaTools.Services.Container`   | This module is responsible for interacting with a special internal storage.                   | [Documentation](API/EN/Container.md)   |
| `TTvActionHub.LuaTools.Services.TwitchTools` | This module is responsible for interacting with Twitch.                                       | [Documentation](API/EN/TwitchTools.md) |
| `TTvActionHub.LuaTools.Stuff.Funcs`          | This module contains useful functions for writing configurations.                             | [Documentation](API/EN/Funcs.md)       |

_Note: I've changed the documentation links to `/EN/` assuming you'll have English versions there. If not, adjust accordingly._

## Example of importing the music playback module

```lua
local Audio = import('TTvActionHub', 'TTvActionHub.LuaTools.Services').Audio
```

## Configuration File Requirements

- File format: The configuration must be a `lua` file.
- Location: The configuration must be located in the `..\configs\` directory.
- Return value: The configuration **must** return a Lua table containing configuration information.
- Encoding: Use UTF-8 encoding for your scripts.
  If any of this is unclear, simply run the program without configuration files; they will be generated automatically.

## Main Configuration Table (file ../configs/config.lua) [Example](example/config.md)

The configuration table contains several parameters:

| Parameter   | Type   | Values     | Description                                                                                               | Optional | Default Value |
| ----------- | ------ | ---------- | --------------------------------------------------------------------------------------------------------- | -------- | ------------- |
| force-relog | `bool` | true/false | If `true`, authorization via browser will be forcibly requested without attempting to update information. | +        | `false`       |
| timeout     | `int`  | Number     | Default cooldown time (in ms) for chat commands if they don't have their own specified.                   | +        | `30000`       |
| logs        | `bool` | true/false | If `true`, internal logs of services related to Twitch will be output.                                    | +        | `false`       |

## Events (file ../configs/twitchevents.lua) [Example](example/twitchevents.md)

The `twitchevents` table contains definitions for your custom events. Each key in this table represents an event name. The value associated with each event name is another table containing the following keys:

| Key     | Type       | Description                                                                                                | Optional | Default Value           |
| ------- | ---------- | ---------------------------------------------------------------------------------------------------------- | -------- | ----------------------- |
| kind    | Event Type | Defines the event type: Command, Channel Points Reward, or other.                                          | -        | -                       |
| action  | Function   | The function that will be executed when the event triggers. It accepts two arguments: `sender` and `args`. | -        | -                       |
| timeout | int        | Cooldown time for the event. Measured in ms. Three possible values: -1, 0, > 0.                            | +        | timeout from config.lua |
| perm    | enum(int)  | Access level for the event.                                                                                | +        | Viewer                  |
| cost    | long       | Defines the cost to trigger the event.                                                                     | +        | 0                       |

The `kind` parameter is of type `TwitchEventKind` and can take one of two values. The structure of the `kind` parameter is shown below.

```csharp
public enum TwitchEventKind : byte
{
    Command = 0, TwitchReward
}
```

Below is an example of usage.

```lua
-- ...
twitchevents['test'] = {}
twitchevents['test']['kind'] = TwitchTools.TwitchEventKind.Command -- or TwitchTools.TwitchEventKind.TwitchReward
-- ...
```

The `action` parameter is a `lua-function`. An example of writing it and using other modules is shown below.

```lua
-- ...
twitchevents['test'] = {}
twitchevents['test']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@' .. sender .. ' -> test')
  end
-- ...
```

The `sender` and `args` parameters accepted by the function:

- `sender` - A string containing the Twitch username of the user who sent this command.
- `args` - An array of strings containing all the words the user wrote when sending this command, split by spaces.

The `perm` parameter is of type `PermissionLevel` and can take one of five values. The structure of the `perm` parameter is shown below.

```csharp
public enum PermissionLevel : int
{
    Viewer, Vip, Subscriber, Moderator, Broadcaster
}
```

Below is an example of usage.

```lua
-- ...
twitchevents['test'] = {}
twitchevents['test']['perm'] = TwitchTools.PermissionLevel.Vip -- or TwitchTools.PermissionLevel.Viewer, etc.
-- ...
```

## Timer Actions (File ../configs/timeractions.lua) [Example](example/timeractions.md)

The `tactions` table contains definitions for your custom timer actions. Each key in this table represents an action name (which you specified in the Twitch panel). The value associated with each action name is another table containing the following keys:

| Key     | Type     | Description                                                                                     | Optional |
| ------- | -------- | ----------------------------------------------------------------------------------------------- | -------- |
| action  | Function | The function that will be executed when the action is triggered. It takes no arguments.         | -        |
| timeout | integer  | The time interval (in ms) after which this action will be triggered. Any value >= 1 is allowed. | -        |

Since the function accepted by the `action` field is simple, it does not take any arguments.

Example of creating a timer action:

```lua
local tactions = {}
-- Some other useful code...
local taction1 = {}
taction1["action"] =
    function()
        Chat.SendMessageAsync("Test")
    end
taction1["timeout"] = 5000 -- This is 5 seconds
tactions["test"] = taction1
```
