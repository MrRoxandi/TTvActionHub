```lua
local Sounds = import("TTvActionHub", "TTvActionHub.LuaTools.Audio").Sounds
local Keyboard = import("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Keyboard
local Mouse = import("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Mouse
local TwitchChat = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").TwitchChat
local Storage = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Storage
local Funcs = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Funcs
local Users = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Users

local rewards = {}

rewards['test'] = {}
rewards['test']['action'] =
    function(sender, args)
        TwitchChat.SendMessag('@' .. sender .. ' -> test')
    end

return rewards

```
