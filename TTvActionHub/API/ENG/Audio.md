# Documentation for the 'Audio' module in `TTvActionHub.LuaTools.Services.Audio`

This module provides functions for playing sounds from local files and via URL, as well as for managing sound volume.

## Importing into the configuration file

Example of importing the module:

```lua
local Audio = import('TTvActionHub', 'TTvActionHub.LuaTools.Services').Audio
```

## Available Methods

| Function                       | Description                                                                                                |
|--------------------------------|------------------------------------------------------------------------------------------------------------|
| `PlaySound(string uri)`        | Asynchronously plays a sound from the specified URL or local file path given by `uri`.                     |
| `SetVolume(float volume)`      | Sets the sound volume. The `volume` value must be in the range from 0.0 (silence) to 1.0 (maximum volume). |
| `GetVolume()`                  | Returns the current sound volume. The value is in the range from 0.0 to 1.0.                               |
| `IncreaseVolume(float volume)` | Increases the sound volume by the specified `volume` value. The volume cannot exceed 1.0.                  |
| `DecreaseVolume(float volume)` | Decreases the sound volume by the specified `volume` value. The volume cannot be less than 0.0.            |
| `SkipSound()`                  | Interrupts the current sound playback.                                                                     |

Example of using methods in the configuration file:

```lua
local Audio = import('TTvActionHub', 'TTvActionHub.LuaTools.Services').Audio

-- Play sound from a local file
Audio.PlaySound("C:/Sounds/mysound.mp3")

-- Play sound from a URL
Audio.PlaySound("https://example.com/audio/sound.ogg")

-- Set volume
Audio.SetVolume(0.5) -- Set volume to 50%

-- Get current volume
local currentVolume = Audio.GetVolume()
print("Current volume: " .. currentVolume)

-- Increase volume by 10%
Audio.IncreaseVolume(0.1)

-- Decrease volume by 20%
Audio.DecreaseVolume(0.2)

-- Skip sound playback
Audio.SkipSound()
```
