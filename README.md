# Action Speed

Makes everyday actions take less game time without giving less, and lets you, the robot and your survivors walk faster.

In Survival Log every action costs game time: eating, using furniture, treating a wound, crafting. Short chores add up and eat your day. Action Speed lets you set how much game time each kind of action takes, and how fast you, the robot and other people at your base walk.

## Features

- **Speed per action type**: every action in the game has a type (furniture use, food, rest, entertainment, exercise, social, treatment, move, other, free, repair). Each type gets a speed: 2 means the action takes half the game time, 0.5 means twice as long. Furniture use, food, treatment and other default to 2. Every other type keeps the game's pace until you set it.
- **Full benefits**: a sped-up action still gives everything it gives at the game's pace. Sleep twice as fast and you wake up as rested as after a full sleep. The mod runs the action's effect faster instead of cutting it short. Turn `FullBenefits` off to cut sped-up actions short instead, so they give only what they give in the shorter time. The progress bar over the character's head fills with the sped-up action.
- **Free second search**: searching a container you have already searched opens it at once. No game time passes, so on an expedition it adds no exposure. Turn `FreeSecondSearch` off to make every search take its time.
- **Overrides by id**: set a speed for one single action by its id. An override beats its type's speed.
- **Every action listed**: the mod writes `BepInEx\action-speed-actions.txt`, listing every action with its id, name, type, the game's time, the time with this mod, and why.
- **Walking speed**: you (the character you control), the robot and other people at your base each have a walking speed, 1.5 times as fast by default. Enemies always walk at the game's speed. The walk animation keeps up with the faster walk.
- **Crafting speed**: crafting at a workbench is timed apart from other actions, so it has its own speed. It defaults to 1, the game's pace, and at 1 the mod does not touch crafting at all. Brewing and cooking speeds are in the Machine Speed mod.
- **Keeps up with the game**: if the game reloads its action table, the mod notices within a few seconds and writes its times back.
- **Log file**: what the mod set up at startup and after each change of a setting, and the first craft, walk, sped-up action effect and free second search it changed, go to `BepInEx\action-speed.log`.

Action Speed changes how much game time actions take. It writes no data of its own into your save, and it does not edit game files. It does change your game's time: with it on, you get more done in a day, and anything you save reflects that. The shortened length of a craft in progress is held by that craft, so it can end up in your save. You can remove the mod at any time; actions started after that take the game's own time again.

Config keys are the game's English type names. Each one says in the config file what kind of actions it covers, in English and Chinese. Action ids work in any game language, and the action list shows names in the language the game is set to.

## How it works

Each action in the game's action table has a length in game time. When an action starts, the game also starts the action's effect, which lasts that length. The effect runs the timers that pay out rest, food and the rest while the action runs, and the action ends when the effect runs out.

With `FullBenefits` on (the default), the mod leaves each action's length as the game set it and runs a sped-up action's effect on a faster clock. At speed 2, every payout timer and the end of the action come twice as fast, so the action ends in half the game time and gives exactly the same. With `FullBenefits` off, the mod writes a shorter length into the action table instead, so the action ends early and stops paying out when it ends.

For each action, highest first:

1. An action the table does not time (crafting, brewing, cooking and some others) is left alone. It shows as "not timed by this table" in the list.
2. An action in `Overrides` takes its override speed.
3. An action whose type speed is 1 keeps the game's time.
4. Every other action takes its type's speed.

With `FreeSecondSearch` on (the default), a search of a container that has been searched before runs its effect so fast that it ends within a few frames, whatever its type speed or override.

Crafting is set when each craft starts.

## Known issues and limits

- Action speeds apply to everyone doing the action, survivors and the robot included, not only you. Walking has its own speed for you, for the robot and for survivors.
- With `FullBenefits` on, a time the game shows for a sped-up action outside the bar over the character's head may show the game's full length, while the action itself ends sooner.
- Effects that last a set time on their own, such as buffs, run on the game's clock as usual. Only the action's own effect runs faster.
- The workbench's progress bar may run at the game's pace when `Crafting` is above 1: the craft finishes early and the bar can jump to done.
- A craft that is already running when you change a speed keeps the length it started with.
- Overrides on crafting, brewing and cooking actions do nothing, because those are not timed by the action table. Use the `[Speed]` setting for crafting.
- Sorting has no speed of its own. The timed "organize items" action (id 11) is in the `Other` type, so it is already sped up there, and `Overrides` can set it alone. Organizing through a container's panel has no fixed length: it lasts as long as the panel is open.
- With `FullBenefits` off, speeding up an action also shortens what its effect gives or takes over that time.

## Requirements

- Survival Log 1.1.18902. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Optional, to change settings without a restart: install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from Nexus Mods, and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `ActionSpeed-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/53).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ActionSpeed.dll`

## Uninstall

Delete `ActionSpeed.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, `BepInEx\action-speed.log` and `BepInEx\action-speed-actions.txt`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. See [CONFIG.md](CONFIG.md) for every setting. The mod has no hotkeys.

## Troubleshooting

Built for Survival Log 1.1.18902 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Action Speed 0.6.0 started`. The line after it lists the action speeds in use, ending for example `full benefits on, free second search on`. Lines below it say what was hooked, such as `full benefits on, sped-up actions give everything they give at the game's pace`, `progress bars of sped-up actions fill with the action`, `walking speed x1.5 for you, x1.5 for the robot, x1.5 for survivors` and `crafting left at the game's pace`. A setting changed on the Mods screen writes its lines again. A line starting `could not hook` means a game update has changed that part of the game, and it stays at the game's pace until the mod is updated.

Once the game has loaded its tables, look for `N of M actions changed, every action is listed in action-speed-actions.txt`.

`BepInEx\action-speed.log` has the same lines with a timestamp each, and it is kept across restarts (past 1 MB, only the newest part is kept). It also records the first craft, walk and sped-up action effect the mod changed, on lines starting `first craft:`, `first walk` and `first sped-up effect:`, each with the game's value and the new one. The first free second search writes `first search of a container searched before, it ends at once`. There is one `first walk` line each for you, the robot and survivors. If a speed seems to do nothing, check for that line.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\action-speed.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/ActionSpeed -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\ActionSpeed\bin\Release\ActionSpeed.dll`.

The unit tests cover the pure logic (how each action's time and effect clock are worked out, that a sped-up action with full benefits pays out the same, who walks at which speed, the speed checks and the scaling of crafts). They need nothing from the game:

```
dotnet test tests/ActionSpeed.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
