# Action Speed settings

Settings live in `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

A speed of 1 is always the game's own pace. 2 takes half the game time, 4 takes a quarter, 0.5 takes twice as long. What `0` does is different in each section, so it is noted under each table.

## Actions

One speed per action type, plus two settings that pick out single actions.

| Setting | Default | What it does |
| --- | --- | --- |
| `FurnitureUse` | `2` | Speed of actions done with a piece of furniture. |
| `Food` | `2` | Speed of food actions. |
| `Treatment` | `2` | Speed of treatment actions. |
| `Other` | `2` | Speed of actions the game files under "other", including the timed "organize items" action (id 11). |
| `Rest` | `1` | Speed of rest actions. |
| `Entertainment` | `1` | Speed of entertainment actions. |
| `Exercise` | `1` | Speed of exercise actions. |
| `Social` | `1` | Speed of social actions. |
| `Move` | `1` | Speed of move actions. |
| `Free` | `1` | Speed of free actions. |
| `RepairFurniture` | `1` | Speed of repair actions. Left at 1 because it has not been checked whether a shorter repair restores less. |
| `KeepTimedEffects` | `true` | Leaves an action at the game's pace when its effect gives or takes stats over time (sleeping, resting, some meals), so speeding it up never shortens what it gives you. `false` speeds those up with the rest of their type. |
| `Overrides` | empty | Speeds for single actions, by id, written `id:speed` and separated by commas, for example `112:4, 39:2`. An override beats its type's speed and `KeepTimedEffects`. |

To find an action's id and type, open `BepInEx\action-speed-actions.txt`. The mod writes it once the game has loaded its tables. Each line reads `id | name | type | game clock -> with this mod | why`, with times in minutes of game time. The last column is one of:

- `Changed`: sped up (or slowed down) by its type's speed.
- `Overridden`: set by `Overrides`.
- `GameSpeed`: its type's speed is 1, so it keeps the game's time.
- `PaysOverTime`: kept at the game's time by `KeepTimedEffects`.
- `TimedElsewhere`: not timed by this table (crafting, brewing, cooking and some others). Its time shows as "not timed by this table", and neither a type speed nor an override changes it. Use the `[Speed]` section for crafting, brewing and cooking.

Type speeds and override speeds are held between 0.25 and 20. A value below 0.25 is raised to 0.25 and a value above 20 is lowered to 20.

What 0 does here:

- A type speed of `0` (or a negative number) is raised to 0.25, which makes that type take four times as long. To leave a type at the game's pace, write `1`.
- An override with a speed of `0` or less, or an entry that is not `id:speed`, is skipped. The log says `ignored override '<entry>', write it as id:speed with a speed above 0` at startup.
- An override of `1` puts that one action back at the game's pace even when its type is sped up.

## Walking

| Setting | Default | What it does |
| --- | --- | --- |
| `Speed` | `1.5` | How fast your character walks. 1 is the game's speed, 2 is twice as fast. Only your own character is affected; survivors and the robot walk at the game's speed. |

`Speed` is held between 0.25 and 5. A value below 0.25 is raised to 0.25 and a value above 5 is lowered to 5. A `0` (or a negative number) is raised to 0.25, a quarter of the game's walking speed. At exactly 1 the mod does not touch walking at all.

## Speed

Crafting, brewing and cooking are timed apart from the action table, so each has its own speed here. At 1 (the default) the mod does not touch that kind of work at all.

| Setting | Default | What it does |
| --- | --- | --- |
| `Crafting` | `1` | Speed of crafting at a workbench. 2 halves the time a craft takes. |
| `Brewing` | `1` | Speed of brewing alcohol. 2 halves the brew's hours. A brew is timed in whole hours, so the result is rounded to the nearest hour and is never less than 1 hour. The brewing panel shows the shortened hours. |
| `Cooking` | `1` | Speed of cooking at a stove or hot pot. 2 halves the cooking time. Fuel burns by how far the cook has got, so a shorter cook burns the same fuel per dish. |

Each is held between 0.25 and 20. A value outside that range is pulled to the nearest end, and the log says so at startup, for example `Speed/Crafting = 50 is outside 0.25 to 20, using 20`.

What 0 does here: `0` or a negative number counts as 1, the game's pace, and the log says so at startup, for example `Speed/Cooking = 0 is not a speed above 0, using the game's pace (1)`.

A craft, brew or cook keeps the length it had when it started, so a change here only affects ones started after the next game start.
