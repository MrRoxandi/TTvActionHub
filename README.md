<div width="100%" display="flex" justify-content="center" align-items="center">
    <table>
    <tr>
        <td>
            <img src="readme-assets/loog.png" alt="Logo" width="100">
        </td>
        <td>
            <h2>TTvActionHub</h2>
        </td>
    </tr>
  </table>
</div>
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/MrRoxandi/TTvActionHub)

> This project consolidates essential Twitch streaming tools into one platform, including music playback, keyboard and mouse emulation, and data storage. All features are activated via Twitch channel points or commands using a personalized chat bot.

## Features

- **TwitchService:**
  - Fully customizable own events though chat commands and Twitch points redemptions
  - Own points system supported
- **AudioService:**
  - Play almost any audio files with this service
  - Also accepts internet links like: `https://example.com/audiofile.mp3`
- **Container:**
  - Stores almost any data under specified name
- **Hardware:**
  - Allows to emulate keyboard actions
  - Allows to emulate mouse actions

## Highlights

- **[LUA Scripting](#lua-scripting)**: Since the project focuses on complete freedom, we provide possibilities and tools, and the user determines what and how it should work. That's why the most user-friendly interface was chosen for this approach.

## Installation (Windows)

1. Ensure you have Windows with .NET 8 installed. [You can download .NET 8 here if needed](https://dotnet.microsoft.com/en-us/download).
2. Download the latest release from the [Releases](https://github.com/MrRoxandi/TwitchController/releases) page.
3. Extract the archive to a desired location.

## Running (Windows)

1. Navigate to the extracted directory.
2. Run the `TwitchController.exe` and let it generate all files.
3. If you want to configure this program, then navigate to `..\configs\`

## LUA Scripting

This project allows you to customize chat commands using LUA scripts. The [`examples`](TTvActionHub/example/) files provides an example files for configuration, commands and other. For a detailed description of the available API, please refer to the [API_ENG](TTvActionHub/API_ENG.md)/[API_RU](TTvActionHub/API_RU.md) .

## License

This project is licensed under the [MIT License](LICENSE.txt).
