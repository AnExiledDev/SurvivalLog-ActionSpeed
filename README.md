# Action Speed

A BepInEx 6 (IL2CPP) plugin for Survival Log that changes how much in-game time each kind of action costs, and how fast your character walks.

## What it does

- **Speed per action type.** Every action in the game has a type (furniture use, food, rest, entertainment, exercise, social, treatment, move, other, free, repair). Each type gets a speed: 2 means the action takes half the in-game time, 0.5 means twice as long. Furniture use, food, treatment and other default to 2; everything else keeps the game's pace until you set it.
- **Actions that pay out over time are left alone by default.** Sleeping, resting and similar actions restore or drain stats on a timer while they run, so shortening them would shrink what they give you. With `KeepTimedEffects` on (the default) those keep the game's pace. Turn it off, or name the action in `Overrides`, to speed them up anyway.
- **Overrides by id.** Set a speed for one action by its id, beating its type and `KeepTimedEffects`.
- **Every action is listed.** On load, `BepInEx/action-speed-actions.txt` lists every action with its id, name, type, the game's time, the time with this mod, and why.
- **Walking speed.** Your character's walking speed gets its own multiplier, default 1.5. Survivors and the robot walk at the game's speed.

## Install

Copy `ActionSpeed.dll` into `BepInEx/plugins`.

## Config

`BepInEx/config/com.dev.survivallog.actionspeed.cfg`:

| Section | Setting | Default | Meaning |
| --- | --- | --- | --- |
| `[Actions]` | `FurnitureUse`, `Food`, `Treatment`, `Other` | 2 | Speed of that action type |
| `[Actions]` | `Rest`, `Entertainment`, `Exercise`, `Social`, `Move`, `Free`, `RepairFurniture` | 1 | Speed of that action type |
| `[Actions]` | `KeepTimedEffects` | true | Keep the game's pace for actions whose effect pays out over time |
| `[Actions]` | `Overrides` | empty | `id:speed` pairs, comma separated, for example `112:4, 39:2` |
| `[Walking]` | `Speed` | 1.5 | Walking speed multiplier for your character |

Action speeds run from 0.25 to 20, walking from 0.25 to 5. Malformed override entries are skipped and named in `BepInEx/action-speed.log`.

## Known limits

- **Crafting, sorting and brewing are not covered.** Those actions are timed somewhere other than the action table, and the list shows them as "not timed by this table". Overrides on them do nothing.
- **Speeds apply to everyone doing the action**, survivors and the robot included, not only your character. Walking speed is the exception: it is yours alone.
- **Repairs keep the game's pace by default** because it has not been checked whether a shorter repair restores less.
- Speeding up an action also shortens anything its effect does over that time when `KeepTimedEffects` is off.
- 0.1.0 has not been run in the game yet.

## Build

```
dotnet build -c Release src/ActionSpeed
dotnet test tests/ActionSpeed.Tests
```

The project expects the game at `D:\Downloads\Survival-Log-AnkerGames\Survival Log`; pass `-p:GameDir=...` for another path.

## License

MIT, see [LICENSE](LICENSE).
