## Документация для модуля 'TwitchChat' в `TTvActionHub.LuaTools.Stuff`

Ну... Тут ничего прям крутого нет, просто отправка сообщений в чат Twitch от вашего имени :/

| Метод                         | Описание                                      |
| ----------------------------- | --------------------------------------------- |
| `SendMessage(string message)` | Отправляет сообщение в чат от вашего аккаунта |

Пример испоользования в файле конфигурации :

```lua
local TwitchChat = import('TTvActionHub', 'TTvActionHub.Stuff').TwitchChat
local name = "Someone"

TwitchChat.SendMessage("Hello " .. name)
```
