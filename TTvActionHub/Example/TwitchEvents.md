```lua
local twitchevents = {}

twitchevents['ping'] = {}
twitchevents['ping']['kind'] = TwitchTools.TwitchEventKind('Command')
twitchevents['ping']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@'..sender..' -> pong')
  end
twitchevents['ping']['timeout'] = 1000 -- 1000 ms
twitchevents['ping']['perm'] = TwitchTools.PermissionLevel('Viewer')

twitchevents['test'] = {}
twitchevents['test']['kind'] = TwitchTools.TwitchEventKind('TwitchReward')
twitchevents['test']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@'..sender..' -> test')
  end


return twitchevents


```
