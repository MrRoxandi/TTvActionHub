local Keyboard = import('TwitchController', 'TwitchController.Hardware').Keyboard
local Mouse = import('TwitchController', 'TwitchController.Hardware').Mouse
local TwitchChat = import('TwitchController', 'TwitchController.Stuff').Chat

local GLOBAL_TIMER = -1
local NO_TIMER = 0

function create(action, timer, desc)
	local r = {}
	r["action"] = action -- Must exist
	r["timeout"] = timer
	r["description"] = desc
	return r
end

local res = {}
res["botname"] = "Roxandi" -- Must exist
res["channel"] = "Roxandi" -- Must exist
--res["token"] = "wudym3b72464z943c4ro9hzoymwxcx" -- Must exist

res["timeout"] = 1000
res["logs"] = false

--res["opening-bracket"] = '<' -- uncomment if you like !command <arg> more than !command (arg)
--res["closing-bracket"] = '>'

local commands = {}
local rewards = {}

-- function(sender, str)
	-- some work
-- end


commands["hello"] = create(function(sender, args)
	TwitchChat.SendMessage("Hello @".. sender .. ".")
end, NO_TIMER, "")

commands["press"] = create(function (sender, args)
	Keyboard.PressKeyAsync(Keyboard.Keys.A)
end, GLOBAL_TIMER, "")

rewards["test"] = create(nil, nil, nil)

res["commands"] = commands
res["rewards"] = rewards
return res


