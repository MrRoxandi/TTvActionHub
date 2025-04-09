## Documentation for the 'Sounds' module in `TTvActionHub.LuaTools.Audio`

This module provides functions for playing sounds from files on disk and via URL, as well as for controlling the sound volume.

### Connecting in the configuration file

Module connection example:

```lua
local Sounds = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds
```

### Functions

| Function                       | Description                                                                                                |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------- |
| `PlaySound(string uri)`        | Asynchronously plays a sound from the specified URL or local file path provided in `uri`.                  |
| `SetVolume(float volume)`      | Sets the sound volume. The `volume` value must be in the range from 0.0 (silence) to 1.0 (maximum volume). |
| `GetVolume()`                  | Returns the current sound volume. The value is in the range from 0.0 to 1.0.                               |
| `IncreaseVolume(float volume)` | Increases the sound volume by the specified `volume` amount. The volume cannot exceed 1.0.                 |
| `DecreaseVolume(float volume)` | Decreases the sound volume by the specified `volume` amount. The volume cannot be less than 0.0.           |
| `SkipSound()`                  | Stops the currently playing sound.                                                                         |

Example usage of methods in the configuration file

```lua
local Sounds = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds

-- Play sound from a file on disk
Sounds.PlaySound("C:/Sounds/mysound.mp3")

-- Play sound from a URL
Sounds.PlaySound("https://example.com/audio/sound.ogg")

-- Set volume
Sounds.SetVolume(0.5) -- Set volume to 50%

-- Get the current volume
local currentVolume = Sounds.GetVolume()
print("Current volume: " .. currentVolume)

-- Increase volume by 10%
Sounds.IncreaseVolume(0.1)

-- Decrease volume by 20%
Sounds.DecreaseVolume(0.2)

-- Stop sound playback
Sounds.SkipSound()
```

```

```
