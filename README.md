# Action Speed

Makes everyday actions take less game time, and lets your character walk faster.

In Survival Log every action costs game time: eating, using furniture, treating a wound, crafting, cooking. Short chores add up and eat your day. Action Speed lets you set how much game time each kind of action takes, and how fast your own character walks.

## Features

- **Speed per action type**: every action in the game has a type (furniture use, food, rest, entertainment, exercise, social, treatment, move, other, free, repair). Each type gets a speed: 2 means the action takes half the game time, 0.5 means twice as long. Furniture use, food, treatment and other default to 2. Every other type keeps the game's pace until you set it.
- **Effects over time are left alone**: sleeping, resting and some meals give or take stats for as long as they run, so shortening them would shrink what they give you. By default these keep the game's pace, whatever their type's speed. Turn `KeepTimedEffects` off, or set one of them in `Overrides`, to speed them up anyway.
- **Overrides by id**: set a speed for one single action by its id. An override beats its type's speed and `KeepTimedEffects`.
- **Every action listed**: the mod writes `BepInEx\action-speed-actions.txt`, listing every action with its id, name, type, the game's time, the time with this mod, and why.
- **Walking speed**: your character walks 1.5 times as fast by default. Survivors and the robot walk at the game's speed. The walk animation keeps up with the faster walk.
- **Crafting, brewing and cooking speed**: these are timed apart from other actions, so each has its own speed: crafting at a workbench, brewing alcohol, and cooking at a stove or hot pot. All three default to 1, the game's pace, and at 1 the mod does not touch them at all. The brewing panel shows the shortened hours.
- **Keeps up with the game**: if the game reloads its action table, the mod notices within a few seconds and writes its times back.
- **Log file**: what the mod set up at startup, and the first craft, brew, cook and walk it changed, go to `BepInEx\action-speed.log`.

Action Speed changes how much game time actions take. It writes no data of its own into your save, and it does not edit game files. It does change your game's time: with it on, you get more done in a day, and anything you save reflects that. The shortened length of a craft, brew or cook in progress is held by that craft, brew or cook, so it can end up in your save. You can remove the mod at any time; actions started after that take the game's own time again.

Config keys are the game's English type names. Action ids work in any game language, and the action list shows names in the language the game is set to.

## How it works

Each action in the game's action table has a length in game time. When the game loads that table, the mod works out a new length for each action from the speeds you set, and writes it into the table in memory. Every action reads its length from there when it starts.

For each action, highest first:

1. An action the table does not time (crafting, brewing, cooking and some others) is left alone. It shows as "not timed by this table" in the list.
2. An action in `Overrides` takes its override speed.
3. An action whose type speed is 1 keeps the game's time.
4. With `KeepTimedEffects` on, an action whose effect gives or takes stats over time keeps the game's time.
5. Every other action takes its type's speed.

Crafting, brewing and cooking are set when each one starts. A brew is timed in whole hours, so its shortened length is rounded to the nearest hour and is never less than 1 hour.

## Known issues and limits

- Speeds change how much game time things take. Game time is finicky in Survival Log: events, sleep and zombie waves all run on it. If anything about timing looks odd, please tell me (see [Feedback](#feedback-and-bug-reports)).
- The crafting, brewing and cooking speeds are new in 0.2.0 and have not been run in the game yet. They are off (1) by default. Back up your save before trying it for the first time.
- Speeds apply to everyone doing the action, survivors and the robot included, not only your character. Walking speed is the exception: it is yours alone.
- The workbench's progress bar may run at the game's pace when `Crafting` is above 1: the craft finishes early and the bar can jump to done.
- When a cook starts at a stove or hot pot, the bar over the cook's head can show the game's full cooking time for a moment, then it switches to the shortened time; the cook itself takes the shortened time.
- A craft, brew or cook that is already running when you change a speed keeps the length it started with.
- Overrides on crafting, brewing and cooking actions do nothing, because those are not timed by the action table. Use the `[Speed]` settings for them.
- Sorting has no speed of its own. The timed "organize items" action (id 11) is in the `Other` type, so it is already sped up there, and `Overrides` can set it alone. Organizing through a container's panel has no fixed length: it lasts as long as the panel is open.
- Repairs keep the game's pace by default, because it has not been checked whether a shorter repair restores less.
- With `KeepTimedEffects` off, speeding up an action also shortens what its effect gives or takes over that time.
- If the mod cannot hook the brewing panel after a game update, brews are still shortened, but the panel shows the game's hours. The log says so at startup.
- If anything looks wrong, please tell me (see [Feedback](#feedback-and-bug-reports)).

## Requirements

- Survival Log 1.1.18317. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Download `ActionSpeed.dll` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/releases/latest) and put it in the `BepInEx\plugins` folder inside the game folder (the folder with the game .exe). Full path example: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ActionSpeed.dll`

## Uninstall

Delete `ActionSpeed.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, `BepInEx\action-speed.log` and `BepInEx\action-speed-actions.txt`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Tested on Survival Log 1.1.18317 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Action Speed 0.2.0 started:`. The rest of that line lists the speeds in use, for example `walking x1.5, crafting x1, brewing x1, cooking x1` at the end. Lines below it say what was hooked, such as `walking speed x1.5 for your character` and `crafting left at the game's pace`. A line starting `could not hook` means a game update has changed that part of the game, and it stays at the game's pace until the mod is updated.

Once the game has loaded its tables, look for `N of M actions changed, every action is listed in action-speed-actions.txt`.

`BepInEx\action-speed.log` has the same lines with a timestamp each, and it is kept across restarts (past 1 MB, only the newest part is kept). It also records the first craft, brew, cook and walk the mod changed, on lines starting `first craft:`, `first brew:`, `first cook:` and `first walk:`, each with the game's value and the new one. If a speed seems to do nothing, check for that line. The `first brew:` line can come from opening the brewing panel, since the panel works out the same hours.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\action-speed.log`.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/ActionSpeed -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\ActionSpeed\bin\Release\ActionSpeed.dll`.

The unit tests cover the pure logic (how each action's time is worked out, the speed checks and the scaling of crafts, brews and cooks). They need nothing from the game:

```
dotnet test tests/ActionSpeed.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
