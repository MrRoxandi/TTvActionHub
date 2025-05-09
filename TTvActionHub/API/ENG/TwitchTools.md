# Documentation for the 'TwitchTools' module in `TTvActionHub.LuaTools.Services.TwitchTools`

Example of importing the module:

```lua
local TwitchTools = import('TTvActionHub', 'TTvActionHub.LuaTools.Services').TwitchTools
```

## Available Methods

| Method                                       | Description                                                                                |
|----------------------------------------------|--------------------------------------------------------------------------------------------|
| `SendMessage(string message)`                | Sends a message to the chat from your account.                                             |
| `SendWhisper(string target, string message)` | Sends a private message (whisper) to the user (`target`) from your account.                |
| `AddPoints(string name, int value)`          | Adds (or subtracts if negative) a certain amount (`value`) of points to the user (`name`). |
| `SetPoints(string name)`                     | Sets certain amount (`value`) of points to the user (`name`).                              |
| `GetPoints(string name)`                     | Gets the current number of points for the user (`name`).                                   |
| `GetEventCost(string eventName)`             | Gets the cost to trigger the event (`eventName`).                                          |

## Additional Data Structures

### PermissionLevel - defines the access level for Twitch events

```csharp
public enum PermissionLevel : int
{
    Viewer, Vip, Subscriber, Moderator, Broadcaster
}
```

### TwitchEventKind - defines the type of Twitch event

```csharp
public enum TwitchEventKind : byte
{
    Command = 0, TwitchReward
}
```

Example of usage in the configuration file:

```lua
-- ...
twitchevents['ping']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@' .. sender .. ' -> pong')
  end
twitchevents['ping']['perm'] = TwitchTools.PermissionLevel.Viewer -- Corrected from VIEWIER
--...
```
