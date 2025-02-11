## Twitch Chat API Methods

## `Sending Messages`

| Метод                              | Описание                                                           |
| ---------------------------------- | ------------------------------------------------------------------ |
| `SendMessage(string message)`      | Отправляет сообщение в чат от вашего аккаунта                      |
| `SendMessageAsync(string message)` | Отправляет сообщение в чат от вашего аккаунта в асинхронном режиме |

### Пример испоользования в `config.lua`:

```lua
local TwitchChat = import('TwitchController', 'TwitchController.Stuff').Chat
local name = "Someone"

TwitchChat.SendMessage("Hello " .. name)
```
