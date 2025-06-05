```lua
local timeractions = {}

timeractions['test'] = {}
timeractions['test']['action'] =
	function()
		TwitchTools.SendMessage('Just a test -> test')
	end
timeractions['test']['timeout'] = 10000 -- 10000 ms

return timeractions

```
