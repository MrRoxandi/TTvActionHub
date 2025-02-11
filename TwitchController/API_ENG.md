## Scripting documentation

This document describes how to write LUA scripts to control your Twitch chat and input interactions.
The following C# classes are available for import into your LUA scripts using the import(assembly, class) function:

## Available imports

### Input control with (`TwitchController.Hardware`)

- Import interface: ([Methods](API/ENG/KeyboardMethods.md))

```lua
local Keyboard = import('TwitchController', 'TwitchController.Hardware').Keyboard
```

- Import interface: ([Methods](API/ENG/MouseMethods.md))

```lua
local Mouse = import('TwitchController', 'TwitchController.Hardware').Mouse
```

### Twitch chat interaction with (`TwitchChatController.Stuff.Twitch`)

- Import interface: ([Methods](API/ENG/ChatMethods.md))

```lua
local TwitchChat = import('TwitchController', 'TwitchController.Stuff').Chat
```

### Some tools for creating scripts with (`TwitchChatController.Stuff.Tools`)

- Import interface: ([Methods](API/ENG/ToolsMethods.md))

```lua
local Tools = import('TwitchController', 'TwitchController.Stuff').Tools
```

## 1. Script requirements

- File format: Scripts must be saved as .lua files.
- Return value: The script **must** return a table containing the configuration. See the "Configuration table" section below for details.
- Encoding: Use UTF-8 encoding for your scripts.

## 2. Configuration Table

The configuration table is the main interface between your script and the application. It must contain the following keys

- channel `(string, required)`: The name of your Twitch channel.
- timeout `(number, optional)`: The default timeout for commands in milliseconds (default: `30,000`).
- logs `(boolean, optional)`: Enables or disables debug information from TwitchLib
- opening-bracket `(string, optional)`: The opening bracket character for command parameters.
- closing-bracket `(string, optional)`: The closing bracket character for command parameters.
- commands `(table, optional)': A table containing the definitions of your custom commands. See the Commands section below. (if not presented will not work!)
- rewards `(table, optional)`: A table containing the definitions of your custom rewards. See the 'Rewards' section below. (if not presenting will not work!)

## 3. Commands

The Commands table contains the definitions of your custom commands. Each key in this table represents a command name (without the "!" prefix). The value associated with each command name is another table containing the following keys

- action `(function, required)`: The function to be executed when the command is triggered. This function takes two parameters:
  - `sender`: A string containing the Twitch nickname of the user who sent this command.
  - `args`: A string array of args sent by the user after the command, separated by spaces.
- timeout `(number, optional)`: Number representing the cooldown for a command
  - values:
    - `-1`: Default cooldown is used instead
    - `0`: No cooldown
    - `100`: 100 milliseconds cooldown
- description `(string, optional)`: Description of the command _(has no usegae for now)_.

## 4. Rewards

The Rewards table contains the definitions of your custom commands. Each key in this table represents a reward title from twitch. The value associated with each command name is another table containing the following keys

- action `(function, required)`: The function to be executed when the command is triggered. This function takes two parameters:
  - `sender`: A string containing the Twitch nickname of the user who sent this command.
  - `args`: A string array of args sent by the user after the command, separated by spaces.
- timeout `(number, optional)`: Number representing the cooldown for a command
  - values:
    - `-1`: Default cooldown is used instead
    - `0`: No cooldown
    - `100`: 100 milliseconds cooldown
- description `(string, optional)`: Description of the command _(has no usegae for now)_.
