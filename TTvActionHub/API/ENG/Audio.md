--- START OF FILE Audio.md ---

# Documentation for the 'Audio' Module

This module provides functions for playing sounds from files on disk and via URL, as well as for managing sound volume.

## Available Methods

| Function                       | Description                                                                                                |
|--------------------------------|------------------------------------------------------------------------------------------------------------|
| `PlaySound(string uri)`        | Asynchronously plays a sound from the specified URL or local file obtained from `uri`.                     |
| `SetVolume(float volume)`      | Sets the sound volume. The `volume` value must be in the range from 0.0 (silence) to 1.0 (maximum volume). |
| `GetVolume()`                  | Returns the current sound volume. The value is in the range from 0.0 to 1.0.                               |
| `IncreaseVolume(float volume)` | Increases the sound volume by the specified `volume` amount. The volume cannot exceed 1.0.                 |
| `DecreaseVolume(float volume)` | Decreases the sound volume by the specified `volume` amount. The volume cannot be less than 0.0.           |
| `SkipSound()`                  | Stops the currently playing sound.                                                                         |

Example of using methods in the configuration file

```lua
-- Play sound from a file on disk
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

-- Stop sound playback
Audio.SkipSound()
```