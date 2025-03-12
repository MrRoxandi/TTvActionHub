## Документация для модуля TwitchChat в `TTvActionHub.LuaTools.Stuff`

Ну... Тут ничего прям крутого нет, просто отправка сообщений в чат Twitch от вашего имени :/

| Метод                              | Описание                                                           |
| ---------------------------------- | ------------------------------------------------------------------ |
| `SendMessage(string message)`      | Отправляет сообщение в чат от вашего аккаунта                      |
| `SendMessageAsync(string message)` | Отправляет сообщение в чат от вашего аккаунта в асинхронном режиме |

Пример испоользования в `config.lua`:

```lua
local TwitchChat = import('TTvActionHub', 'TTvActionHub.Stuff').TwitchChat
local name = "Someone"

TwitchChat.SendMessage("Hello " .. name)
```
