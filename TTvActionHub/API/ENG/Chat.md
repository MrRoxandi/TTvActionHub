## Documentation for the 'TwitchChat' module in `TTvActionHub.LuaTools.Stuff`

Well... There's nothing really fancy here, just sending messages to Twitch chat from your account :/

| Method                        | Description                                   |
| ----------------------------- | --------------------------------------------- |
| `SendMessage(string message)` | Sends a message to the chat from your account |

Usage example in the configuration file:

```lua
local TwitchChat = import('TTvActionHub', 'TTvActionHub.Stuff').TwitchChat
local name = 'Someone'

TwitchChat.SendMessage('Hello ' .. name)
```
