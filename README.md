# Action Speed

Makes everyday actions take less game time while still giving their full benefit, and lets you, the robot and your survivors walk and work faster.

In Survival Log every action costs game time: eating, using furniture, treating a wound, crafting. Short chores add up and eat your day. Action Speed lets you set how much game time each kind of action takes, and how fast you, the robot and other people at your base walk.

## Features

- **A speed for each kind of action**: 38 settings such as meals, snacks, sleep, naps, reading, toilet, cleaning, harvesting, repairs, searching and traps, sorted into ten sections (eating, health, hygiene, rest, leisure, chores, farming, building, scavenging, defense). Each section has an `All` setting that speeds up the whole section, and one `Everything` setting speeds up every action. 2 means the action takes half the game time, 0.5 means twice as long. Every setting starts at the game's pace, so nothing changes until you raise the speeds you want.
- **Full benefits**: a sped-up action still gives everything it gives at the game's pace. Sleep twice as fast and you wake up as rested as after a full sleep. The mod runs the action's effect faster instead of cutting it short. Turn `FullBenefits` off if you'd rather have sped-up actions end early and give only what they would in the shorter time. The progress bar over the character's head keeps up with the faster action.
- **Free second search**: searching a container you have already searched opens it at once. No game time passes, so on an expedition it adds no exposure. It's off by default. Turn `FreeSecondSearch` on to use it.
- **Reading**: one speed for reading books, magazines, newspapers, notes and letters.
- **Overrides by id**: set a speed for one single action by its id. An override beats every speed setting.
- **Every action listed**: the mod writes `BepInEx\action-speed-actions.txt`, listing every action with its id, its name, the setting it follows, the game's time, the time with this mod, and the reason for that time.
- **Walking speed**: you (the character you control), the robot and other people at your base each get their own walking speed. Each starts at 1, the game's speed. Enemies always walk at the game's speed. The walk animation keeps up with the faster walk.
- **Work speed**: you, the robot and other people at your base each get a work speed that speeds up every action they do, on top of each action's own speed. The action still gives its full effect. Each starts at 1, the game's pace. Raiders and creatures always keep the game's pace.
- **Expedition travel**: one speed for the trip between home and an expedition place, going out and coming home. The map shows the new travel time. It starts at 1, the game's pace.
- **Crafting speed**: the game times crafting at a workbench separately from other actions, so it has its own speed. It starts at 1, the game's pace, and at 1 the mod doesn't touch crafting at all. Brewing and cooking speeds are in the Machine Speed mod.
- **Older settings carried over**: if your settings file is from 0.7 or older, its speeds move to the new settings the first time you start the game with 0.8 or newer, so every action keeps the speed it had.
- **Keeps up with the game**: if the game reloads its action table, the mod notices within a few seconds and puts its times back.
- **Log file**: the mod writes `BepInEx\action-speed.log`. It says what the mod set up when the game started and after each setting change, and notes the first craft, walk, sped-up action and free second search it changed.

Action Speed doesn't write any data of its own into your save, and it doesn't edit game files. It does change how much you get done in a day, and your save reflects that. A craft that is running when you save keeps its shorter time in the save. You can remove the mod at any time, and actions started after that take the game's time again.

Each setting in the config file says what it covers, in English and Chinese. Action ids work in any game language, and the action list shows names in the language the game is set to.

## How it works

Each action in the game's action table has a length in game time. When an action starts, the game also starts the action's effect, which lasts that length. While the action runs, the effect pays out things like rest and food, and the action ends when the effect runs out.

With `FullBenefits` on (the default), the mod leaves each action's length as the game set it and runs a sped-up action's effect on a faster clock. At speed 2, every payout timer and the end of the action come twice as fast, so the action ends in half the game time and gives exactly the same. With `FullBenefits` off, the mod writes a shorter length into the action table instead, so the action ends early and stops paying out when it ends.

Each action gets its speed from the first rule that fits:

1. An action the table doesn't time (crafting, brewing, cooking and some others) is left alone. It shows as "not timed by this table" in the list.
2. An action in `Overrides` takes its override speed.
3. Every other action takes the speed of the setting it belongs to: its own setting if that is above 0, otherwise its section's `All` if that is above 0, otherwise `Everything`. At 1 it keeps the game's time.

With `FreeSecondSearch` on, searching a container that has been searched before ends within a few frames, whatever its speed or override.

A craft gets its speed when it starts.

## Known issues and limits

- Action speeds apply to everyone doing the action, survivors and the robot included, not only you. Walking has its own speed for you, for the robot and for survivors.
- With `FullBenefits` on, the bar over the character's head keeps up with a sped-up action, but other times the game shows for it may still show the full length, even though the action ends sooner.
- Buffs and other effects with a timer of their own run at the game's pace as usual. Only the action itself runs faster.
- The workbench's progress bar may run at the game's pace when `Crafting` is above 1: the craft finishes early and the bar can jump to done.
- A craft that is already running when you change a speed keeps the length it started with.
- Overrides on crafting, brewing and cooking actions do nothing, because those are not timed by the action table. Use the `[Speed]` setting for crafting.
- Organizing through a container's panel has no fixed length: it lasts as long as the panel is open, so `Organizing` only speeds up the timed actions such as Organize Items.
- With `FullBenefits` off, speeding up an action also shortens what its effect gives or takes over that time.

## Requirements

- Survival Log 1.1.20719. It's built for this version, so a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.19.0 or newer. Action Speed does not load without it. It applies settings changed on the Mod Manager's Mods screen without restarting the game, and lets each world keep its own speed settings.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `ActionSpeed-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/53).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ActionSpeed.dll`

## Uninstall

Delete `ActionSpeed.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, `BepInEx\action-speed.log`, `BepInEx\action-speed.log.1` and `BepInEx\action-speed-actions.txt`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Each world can keep its own speed settings, except `Overrides`, which is the same for every world. Pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mods screen. See [CONFIG.md](CONFIG.md) for every setting. Every setting starts at the game's pace, so a new install changes nothing until you raise the speeds you want. The mod has no hotkeys.

## Troubleshooting

Built for Survival Log 1.1.20719 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Action Speed 0.10.2 started`. The line after it lists the action speeds in use, such as `most actions x2, except Leisure/Reading x10`, then for example `full benefits on, free second search off, work x1 for you, x1 for the robot, x1 for survivors`. Lines below it say what was hooked, such as `full benefits on, sped-up actions give everything they give at the game's pace`, `progress bars of sped-up actions fill with the action`, `walking speed x1.5 for you, x1 for the robot, x1 for survivors`, `expedition travel x2` and `crafting left at the game's pace`. A setting changed on the Mods screen writes its lines again. A line starting `could not hook` means a game update has changed that part of the game, and it stays at the game's pace until the mod is updated.

Once the game has loaded its tables, look for `N of M actions changed, every action is listed in action-speed-actions.txt`.

`BepInEx\action-speed.log` has the same lines with a timestamp on each, and it is kept across restarts. Once it passes 1 MB, it is renamed to `action-speed.log.1` and a new one starts. It also records the first craft, walk and sped-up action the mod changed, on lines starting `first craft:`, `first walk` and `first sped-up effect:`, each with the game's value and the new one. There is one `first walk` line each for you, the robot and survivors. The first free second search writes `first search of a container searched before, it ends at once`. If a speed seems to do nothing, look for its `first` line.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also post in the Posts tab of the mod's Nexus Mods page. When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\action-speed.log`.

To reach me directly, message `exiled.dev` on Discord. You can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/ActionSpeed -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. Compiling also needs Exiled's Toolkit 0.19.0 or newer: point `-p:ToolkitDll="C:\path\to\ExiledToolkit.dll"` at its DLL. The output DLL is at `src\ActionSpeed\bin\Release\ActionSpeed.dll`.

The unit tests cover the pure logic (which setting each action follows, how old settings move to the new ones, how each action's time and effect clock are worked out, that a sped-up action with full benefits pays out the same, who walks at which speed, the speed checks and the scaling of crafts). They need nothing from the game:

```
dotnet test tests/ActionSpeed.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
