# Документация для модуля 'TwitchTools' в `TTvActionHub.LuaTools.Services.TwitchTools`

Пример подключение модуля:

```lua
local TwitchTools = import('TTvActionHub', 'TTvActionHub.LuaTools.Services').TwitchTools
```

## Доступные методы

| Метод                                        | Описание                                                                       |
| -------------------------------------------- | ------------------------------------------------------------------------------ |
| `SendMessage(string message)`                | Отправляет сообщение в чат от вашего аккаунта                                  |
| `SendWhisper(string target, string message)` | Отправляет личное сообщение сообщение пользователю (target) от вашего аккаунта |
| `AddPoints(string name, int value)`          | Добавить (отнять) некоторое количество (value) очков у пользователя (name).    |
| `GetPoints(string name)`                     | Получить текущее количество очков пользователя (name)                          |
| `GetEventCost(string eventName)`             | Получить стоимость выполнения ивента (eventName)                               |

## Дополнительные структуры данных

### PermissionLevel - определяет уровень доступа к событиям твича

```cs
public enum PermissionLevel : int
{
    Viewer, Vip, Subscriber, Moderator, Broadcaster
}
```

### TwitchEventKind - определяет тип события твича

```cs
public enum TwitchEventKind : byte
{
    Command = 0, TwitchReward
}
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
