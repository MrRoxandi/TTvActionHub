# Документация для модуля 'TwitchTools' в `TTvActionHub.LuaTools.Stuff`

Пример подключение модуля:

```lua
local TwitchTools = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').TwitchTools
```

## Доступные методы

| Метод                                        | Описание                                                               |
| -------------------------------------------- | ---------------------------------------------------------------------- |
| `SendMessage(string message)`                | Отправляет сообщение в чат от вашего аккаунта                          |
| `SendWhisper(string target, string message)` | Отправляет личное сообщение сообщение цели (target) от вашего аккаунта |

## Необходимые дополнительные данные

```cs
enum PermissionLevel { VIEWIER, VIP, SUB, MODERATOR, BROADCASTER }
```

```cs
enum TwitchEventKind { Command , TwitchReward, PointsReward }
```

Пример испоользования в файле конфигурации :

```lua
-- ...
twitchevents['ping']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@' .. sender .. ' -> pong')
  end
twitchevents['ping']['perm'] = TwitchTools.PermissionLevel.VIEWIER
--...

```
