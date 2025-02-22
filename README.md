# **DEV BRANCH README AND API DOCS DEPRECATED!!!!!!**

# **ДЛЯ ДЕВ ВЕТКИ РЕДМИ И АПИ ДОКИ НЕ АКТУАЛЬНЫ!!!!!!**

# Twitch Controller ()

> Spice up your Twitch streams by letting your chat control your keyboard and mouse with custom LUA scripts!

This project lets you create custom Twitch chat commands that trigger keyboard and mouse presses. Viewers can use these commands or rewards for channel points to interact with your game or stream. Define the command logic keyboard and mouse actions using LUA scripts.

## Features

- **Custom Chat Commands:** Create commands that viewers can trigger in chat.
- **LUA Scripting:** Define command behavior and keyboard actions with flexible LUA scripts.
- **Keyboard Emulation:** Simulate key presses and combinations for in-game actions or other effects.
- **Mouse emulation:** Simulate mouse buttons presses and wheel scrolls for in-game actions or other effects.

## Installation (Windows)

1. Ensure you have Windows with .NET 8 installed. [You can download .NET 8 here if needed](https://dotnet.microsoft.com/en-us/download).
2. Download the latest release from the [Releases](https://github.com/MrRoxandi/TwitchController/releases) page.
3. Extract the archive to a desired location.

## Running (Windows)

1. Navigate to the extracted directory.
2. Place `config.lua` file in bin directory.
3. Run the `TwitchController.exe`.

## LUA Scripting

This project allows you to customize chat commands using LUA scripts. The [`example.lua`](TwitchController/example/example.md) file provides an example configuration. For a detailed description of the available API, please refer to the [API_ENG](TwitchController/API_ENG.md)/[API_RU](TwitchController/API_RU.md) .

## License

This project is licensed under the [MIT License](LICENSE.txt).
