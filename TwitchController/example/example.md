```lua
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
end -- Same for commands and rewards

local res = {}

res["channel"] = "YourTwitchChannel" -- Must exist

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
Keyboard.TypeKeyAsync(Keyboard.KeyCode.A)
end, GLOBAL_TIMER, "")

commands["move"] = create(function (sender, args)
Mouse.MoveAsync(100, 100)
end, GLOBAL_TIMER, "")

commands["hold"] = create(function(sender, args)
Keyboard.HoldKeyAsync(Keyboard.KeyCode.A, 25000)
end, GLOBAL_TIMER, "")

commands["scroll"] = create(function(sender, args)
Mouse.Scroll(10)
end, GLOBAL_TIMER, "")

commands["rbm"] = create(function ( sender, args )
Mouse.Click(Mouse.MouseButton.Right)
end, GLOBAL_TIMER, "")

rewards["test"] = create(function(sender, args)
TwitchChat.SendMessage(sender .. " -> " .. args[0])
end, nil, nil)

res["commands"] = commands
res["rewards"] = rewards
return res
```
