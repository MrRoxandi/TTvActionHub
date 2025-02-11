## Twitch Chat API Methods

## `Sending Messages`

| Method                             | Description                                              |
| ---------------------------------- | -------------------------------------------------------- |
| `SendMessage(string message)`      | Sends a message to chat from your account                |
| `SendMessageAsync(string message)` | Sends a message to chat from your account asynchronously |

### Example usage in `config.lua`:

```lua
local TwitchChat = import("TwitchController", "TwitchController.Stuff").Chat
local name = "Someone"

TwitchChat.SendMessage("Hello" .. name)
```
