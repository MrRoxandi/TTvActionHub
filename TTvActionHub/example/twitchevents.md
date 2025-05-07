```lua
local Sounds = import ("TTvActionHub", "TTvActionHub.LuaTools.Audio").Sounds
local Keyboard = import ("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Keyboard
local Mouse = import ("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Mouse
local TwitchTools = import ("TTvActionHub", "TTvActionHub.LuaTools.Stuff").TwitchTools
local Storage = import ("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Storage
local Funcs = import ("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Funcs

local twitchevents = {}

twitchevents['ping'] = {}
twitchevents['ping']['kind'] = TwitchTools.TwitchEventKind.Command
twitchevents['ping']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@'..sender..' -> pong')
  end
twitchevents['ping']['timeout'] = 1000 -- 1000 ms
twitchevents['ping']['perm'] = TwitchTools.PermissionLevel.VIEWIER

twitchevents['test'] = {}
twitchevents['test']['kind'] = TwitchTools.TwitchEventKind.TwitchReward
twitchevents['test']['action'] =
  function(sender, args)
    TwitchTools.SendMessage('@'..sender..' -> test')
  end


return twitchevents


```
