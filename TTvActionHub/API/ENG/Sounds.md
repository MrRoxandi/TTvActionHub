## Documentation for the Sounds Module in `TTvActionHub.LuaTools.Audio`

This module provides functions for playing sounds from files on disk and from URLs, as well as for controlling the sound volume.

### Functions

| Function                       | Description                                                                                                 |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| `PlaySoundAsync(string uri)`   | Asynchronously plays audio at the specified URL-address or a local file obtained from the uri.              |
| `SetVolume(float volume)`      | Sets the sound volume. The `volume` value should be in the range from 0.0 (silent) to 1.0 (maximum volume). |
| `GetVolume()`                  | Returns the current sound volume. The value is in the range from 0.0 to 1.0.                                |
| `IncreeseVolume(float volume)` | Increases the sound volume by the specified value `volume`. The volume cannot exceed 1.0.                   |
| `DecreeseVolume(float volume)` | Decreases the sound volume by the specified value `volume`. The volume cannot be less than 0.0.             |
| `SkipSound()`                  | Interrupts the current sound playback.                                                                      |

**Notes:**

- All functions ending with `Async` are executed asynchronously, without blocking the main program thread.
- Before using any of these functions, make sure that the `audio` service has been initialized. If `audio` is `null`, an exception will be thrown.
- Supported sound file formats depend on the NAudio library used. Typically these are WAV, MP3, Ogg Vorbis, and others.

### Example usage in `config.lua`

```lua
local Sounds = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds

-- Playing a sound from a file on disk
Sounds.PlaySoundAsync("C:/Sounds/mysound.mp3")

-- Playing a sound from a URL
Sounds.PlaySoundAsync("https://example.com/audio/sound.ogg")

-- Setting the volume
Sounds.SetVolume(0.5) -- Setting the volume to 50%

-- Getting the current volume
local currentVolume = Sounds.GetVolume()
print("Current volume: " .. currentVolume)

-- Increasing the volume by 10%
Sounds.IncreeseVolume(0.1)

-- Decreasing the volume by 20%
Sounds.DecreeseVolume(0.2)

-- Interrupting sound playback
Sounds.SkipSound()
```
