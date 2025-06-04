```lua
local Sounds = import ("TTvActionHub", "TTvActionHub.LuaTools.Audio").Sounds
local Keyboard = import ("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Keyboard
local Mouse = import ("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Mouse
local TwitchChat = import ("TTvActionHub", "TTvActionHub.LuaTools.Stuff").TwitchChat
local Storage = import ("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Storage
local Funcs = import ("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Funcs
local Users = import ("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Users

local timeractions = {}

timeractions['test'] = {}
timeractions['test']['action'] =
	function()
		TwitchChat.SendMessage('Just a test -> test')
	end
timeractions['test']['timeout'] = 10000 -- 10000 ms

return timeractions

```
