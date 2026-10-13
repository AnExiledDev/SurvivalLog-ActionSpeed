# Action Speed

Makes everyday actions take less game time without giving less, and lets you, the robot and your survivors walk faster.

In Survival Log every action costs game time: eating, using furniture, treating a wound, crafting, cooking. Short chores add up and eat your day. Action Speed lets you set how much game time each kind of action takes, and how fast you, the robot and the other survivors at your base walk.

## Features

- **Speed per action type**: every action in the game has a type (furniture use, food, rest, entertainment, exercise, social, treatment, move, other, free, repair). Each type gets a speed: 2 means the action takes half the game time, 0.5 means twice as long. Furniture use, food, treatment and other default to 2. Every other type keeps the game's pace until you set it.
- **Full benefits**: a sped-up action still gives everything it gives at the game's pace. Sleep twice as fast and you wake up as rested as after a full sleep; eat twice as fast and you are just as full. The mod runs the action's effect faster instead of cutting it short. Turn `FullBenefits` off to cut sped-up actions short instead, so they give only what they give in the shorter time.
- **Overrides by id**: set a speed for one single action by its id. An override beats its type's speed.
- **Every action listed**: the mod writes `BepInEx\action-speed-actions.txt`, listing every action with its id, name, type, the game's time, the time with this mod, and why.
- **Walking speed**: you (the character you control), the robot and the other survivors at your base each have a walking speed, 1.5 times as fast by default. Enemies always walk at the game's speed. The walk animation keeps up with the faster walk.
- **Crafting, brewing and cooking speed**: these are timed apart from other actions, so each has its own speed: crafting at a workbench, brewing alcohol, and cooking at a stove or hot pot. All three default to 1, the game's pace, and at 1 the mod does not touch them at all. The brewing panel shows the shortened hours.
- **Keeps up with the game**: if the game reloads its action table, the mod notices within a few seconds and writes its times back.
- **Log file**: what the mod set up at startup, and the first craft, brew, cook, walk and sped-up action effect it changed, go to `BepInEx\action-speed.log`.

Action Speed changes how much game time actions take. It writes no data of its own into your save, and it does not edit game files. It does change your game's time: with it on, you get more done in a day, and anything you save reflects that. The shortened length of a craft, brew or cook in progress is held by that craft, brew or cook, so it can end up in your save. You can remove the mod at any time; actions started after that take the game's own time again.

Config keys are the game's English type names. Each one says in the config file what kind of actions it covers, in English and Chinese. Action ids work in any game language, and the action list shows names in the language the game is set to.

## How it works

Each action in the game's action table has a length in game time. When an action starts, the game also starts the action's effect, which lasts that length. The effect runs the timers that pay out rest, food and the rest while the action runs, and the action ends when the effect runs out.

With `FullBenefits` on (the default), the mod leaves each action's length as the game set it and runs a sped-up action's effect on a faster clock. At speed 2, every payout timer and the end of the action come twice as fast, so the action ends in half the game time and gives exactly the same. With `FullBenefits` off, the mod writes a shorter length into the action table instead, so the action ends early and stops paying out when it ends.

For each action, highest first:

1. An action the table does not time (crafting, brewing, cooking and some others) is left alone. It shows as "not timed by this table" in the list.
2. An action in `Overrides` takes its override speed.
3. An action whose type speed is 1 keeps the game's time.
4. Every other action takes its type's speed.

Crafting, brewing and cooking are set when each one starts. A brew is timed in whole hours, so its shortened length is rounded to the nearest hour and is never less than 1 hour.

## Known issues and limits

- Speeds change how much game time things take. Game time is finicky in Survival Log: events, sleep and zombie waves all run on it. If anything about timing looks odd, please tell me (see [Feedback](#feedback-and-bug-reports)).
- Full benefits, the robot's walking speed and the survivors' walking speed have not been run in the game yet, and neither have the crafting, brewing and cooking speeds. Back up your save before trying the mod for the first time.
- Action speeds apply to everyone doing the action, survivors and the robot included, not only you. Walking has its own speed for you, for the robot and for survivors.
- With `FullBenefits` on, a bar or time the game shows for a sped-up action may show the game's full length, while the action itself ends sooner.
- Effects that last a set time on their own, such as buffs, run on the game's clock as usual. Only the action's own effect runs faster.
- The workbench's progress bar may run at the game's pace when `Crafting` is above 1: the craft finishes early and the bar can jump to done.
- When a cook starts at a stove or hot pot, the bar over the cook's head can show the game's full cooking time for a moment, then it switches to the shortened time; the cook itself takes the shortened time.
- A craft, brew or cook that is already running when you change a speed keeps the length it started with.
- Overrides on crafting, brewing and cooking actions do nothing, because those are not timed by the action table. Use the `[Speed]` settings for them.
- Sorting has no speed of its own. The timed "organize items" action (id 11) is in the `Other` type, so it is already sped up there, and `Overrides` can set it alone. Organizing through a container's panel has no fixed length: it lasts as long as the panel is open.
- Repairs keep the game's pace by default, because it has not been checked whether a shorter repair restores less.
- With `FullBenefits` off, speeding up an action also shortens what its effect gives or takes over that time.
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

Check `BepInEx\LogOutput.log` for `===== Action Speed 0.3.0 started:`. The rest of that line lists the speeds in use, for example `full benefits on, walking x1.5 (robot x1.5, survivors x1.5), crafting x1, brewing x1, cooking x1` at the end. Lines below it say what was hooked, such as `full benefits on, sped-up actions give everything they give at the game's pace`, `walking speed x1.5 for you, x1.5 for the robot, x1.5 for survivors` and `crafting left at the game's pace`. A line starting `could not hook` means a game update has changed that part of the game, and it stays at the game's pace until the mod is updated.

Once the game has loaded its tables, look for `N of M actions changed, every action is listed in action-speed-actions.txt`.

`BepInEx\action-speed.log` has the same lines with a timestamp each, and it is kept across restarts (past 1 MB, only the newest part is kept). It also records the first craft, brew, cook, walk and sped-up action effect the mod changed, on lines starting `first craft:`, `first brew:`, `first cook:`, `first walk` and `first sped-up effect:`, each with the game's value and the new one. There is one `first walk` line each for you, the robot and survivors. If a speed seems to do nothing, check for that line. The `first brew:` line can come from opening the brewing panel, since the panel works out the same hours.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\action-speed.log`.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/ActionSpeed -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\ActionSpeed\bin\Release\ActionSpeed.dll`.

The unit tests cover the pure logic (how each action's time and effect clock are worked out, that a sped-up action with full benefits pays out the same, who walks at which speed, the speed checks and the scaling of crafts, brews and cooks). They need nothing from the game:

```
dotnet test tests/ActionSpeed.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
