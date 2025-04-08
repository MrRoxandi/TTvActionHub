```lua
local Sounds = import("TTvActionHub", "TTvActionHub.LuaTools.Audio").Sounds
local Keyboard = import("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Keyboard
local Mouse = import("TTvActionHub", "TTvActionHub.LuaTools.Hardware").Mouse
local TwitchChat = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").TwitchChat
local Storage = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Storage
local Funcs = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Funcs
local Users = import("TTvActionHub", "TTvActionHub.LuaTools.Stuff").Users

local commands = {}

commands['test'] = {}
commands['test']['action'] =
    function(sender, args)
        TwitchChat.SendMessage('@' .. sender .. ' -> test')
    end
commands['test']['timeout'] = 1000 -- 1000 ms
commands['test']['perm'] = Users.USERLEVEL.VIEWIER

commands['sing'] = {}
commands['sing']['action'] =
    function(sender, args)
        if not args then
            TwitchChat.SendMessage('@' .. sender .. ' usage: !sing <link>')
            return
        end
        local link = args[0]
        Sounds.PlaySound(link)
    end
commands['sing']['timeout'] = 10000

commands['skip'] = {}
commands['skip']['action'] =
    function(sender, args)
        Sounds.SkipSound()
    end
commands['skip']['timeout'] = 10000

commands['typemsg'] = {}
commands['typemsg']['action'] =
    function(sender, args)
        if args == nil then
            TwitchChat.SendMessage('@' .. sender .. ' -> usage: !typemsg <message>')
        else
            local message = Funcs.CollectionToString(args, ' ')
            Keyboard.TypeMessage(message)
        end
    end
commands['typemsg']['timeout'] = 1000

commands['close'] = {}
commands['close']['action'] =
    function(sender, args)
        Keyboard.PressKey(Keyboard.Key.Alt)
        Keyboard.PressKey(Keyboard.Key.F4)
        Keyboard.ReleaseKey(Keyboard.Key.Alt)
        Keyboard.ReleaseKey(Keyboard.Key.F4)
    end
commands['close']['timeout'] = 10390953

return commands

```
