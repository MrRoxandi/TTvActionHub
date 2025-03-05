```lua
local Keyboard = import('TwitchController', 'TwitchController.LuaTools.Hardware').Keyboard
local Mouse = import('TwitchController', 'TwitchController.LuaTools.Hardware').Mouse
local TwitchChat = import('TwitchController', 'TwitchController.LuaTools.Stuff').Chat
local Funcs = import('TwitchController', 'TwitchController.LuaTools.Stuff').Funcs
local Storage = import('TwitchController', 'TwitchController.LuaTools.Stuff').Storage
local Audio = import('TwitchController', 'TwitchController.LuaTools.Audio').Sounds

local GLOBAL_TIMER = -1
local NO_TIMER = 0

function create(action, timer)
	local r = {}
	r["action"] = action -- Must exist
	r["timeout"] = timer
	return r
end -- Same for commands and rewards

local res = {}

res["force-relog"] = false
res["timeout"] = 1000
res["logs"] = false

--res["opening-bracket"] = '<' -- uncomment if you like !command <arg> more than !command (arg)
--res["closing-bracket"] = '>'

local commands = {}
local rewards = {}

-- function(sender, str)
	-- some work
-- end

commands["playsong"] = create(
	function(sender, args)
		Audio.PlaySoundFromUrlAsync("https://example.com/example.mp3")
	end, nil
)

commands["volume"] = create(
	function(sender, args)
		if (args == nil) then
			Chat.SendMessageAsync("@".. sender .. " current volume is " .. Audio.GetVolume())
		else
			Audio.SetVolume(tonumber(args[0]))
		end
	end, nil
)

commands["skipsong"] = create(
	function(sender, args)
		Audio.SkipSound()
	end, nil
)

commands["bucket"] = create(
    function(sender, args)
        if(args == nil) then
            local result = Storage.GetStringAsync("bucket").Result
            if(result == nil) then
                Chat.SendMessageAsync("@"..sender.." bucket is not created yet.")
            else
                Chat.SendMessageAsync("@"..sender.." bucket stores: ".. result)
            end
        else
            Storage.InsertStringAsync("bucket", Funcs.CollectionToStringAsync(args).Result)
        end
    end, nil
)

commands["counter"] = create(
    function(sender, args)
        if(args == nil) then
            local result = Storage.GetIntAsync("counter").Result;
            if(result == nil) then
                Chat.SendMessageAsync("@"..sender.." counter is not created yet.")
            else
                Chat.SendMessageAsync("@"..sender.." counter is ".. tostring(result))
            end
        else
            local value = tonumber(args[0])
            local old = Storage.GetInt("counter");
            if(old == nil) then
                Storage.InsertInt("counter", value);
            else
                Storage.InsertIntAsync("counter", value + old)
            end
        end
    end, nil
)

res["commands"] = commands
res["rewards"] = rewards

return res
```
