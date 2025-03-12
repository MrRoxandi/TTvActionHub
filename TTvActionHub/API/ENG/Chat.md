## Documentation for the TwitchChat Module in `TTvActionHub.LuaTools.Stuff`

Well... There's nothing particularly amazing here, just sending messages to the Twitch chat on your behalf :/

| Method                             | Description                                                        |
| ---------------------------------- | ------------------------------------------------------------------ |
| `SendMessage(string message)`      | Sends a message to the chat from your account                      |
| `SendMessageAsync(string message)` | Sends a message to the chat from your account in asynchronous mode |

Example usage in `config.lua`:

```lua
local TwitchChat = import('TTvActionHub', 'TTvActionHub.Stuff').TwitchChat
local name = "Someone"

TwitchChat.SendMessage("Hello " .. name)
```
