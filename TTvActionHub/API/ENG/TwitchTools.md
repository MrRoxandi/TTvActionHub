# Documentation for the 'TwitchTools' Module

## Available Methods

| Method                                       | Description                                                                  |
|----------------------------------------------|------------------------------------------------------------------------------|
| `SendMessage(string message)`                | Sends a message to the chat from your account                                |
| `SendWhisper(string target, string message)` | Sends a private message (whisper) to the user (target) from your account     |
| `AddPoints(string name, int value)`          | Adds (or subtracts) a certain amount (value) of points for the user (name).  |
| `SetPoints(string name, int value)`          | Sets a certain amount (value) of points for the user (name).                 |
| `GetPoints(string name)`                     | Gets the current point balance of the user (name).                           |
| `GetEventCost(string eventName)`             | Gets the cost of triggering the event (eventName).                           |

## Additional Data Structures

### PermissionLevel - defines the access level for Twitch events

```cs
public enum PermissionLevel : int
{
    Viewer, Vip, Subscriber, Moderator, Broadcaster
}
```

### TwitchEventKind - defines the type of Twitch event

```cs
public enum TwitchEventKind : byte
{
    Command = 0, TwitchReward
}
```

Example usage in the configuration file:

```lua
-- ...
twitchevents['ping']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@' .. sender .. ' -> pong')
  end
twitchevents['ping']['perm'] = TwitchTools.PermissionLevel('Viewer')
--...

```