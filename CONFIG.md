# Action Speed settings

Settings live in `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back. Each setting in the file says what it does, in English and Chinese. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

A speed of 1 is always the game's own pace. 2 takes half the game time, 4 takes a quarter, 0.5 takes twice as long. What `0` does is different in each section, so it is noted under each table.

## Actions

One speed per action type, plus four settings: one for reading, one for full benefits, one for single actions and one for searching a container again.

| Setting | Default | What it does |
| --- | --- | --- |
| `FurnitureUse` | `2` | Speed of furniture actions, such as unpacking furniture packages, Take Apart Chime, Break Trinket, Deploy Rescue Beacon, Upgrade Gate and Try Cooking. |
| `Food` | `2` | Speed of eating and drinking, such as Eat Cookies, Eat Burger, Drink Coffee, Enjoy Hot Pot and Eat Food. |
| `Treatment` | `2` | Speed of taking medicine: Eat Vitamins and Take Meds. |
| `Other` | `2` | Speed of everything else, such as Search, Harvest, Pry Open, Wash Hands, Use Toilet, Unlock Furniture, Planting, the timed "organize items" action (id 11) and short moments like Hungry... and Tired... |
| `Rest` | `1` | Speed of sleeping and resting, such as Sleep, Nap, Play Video Games, Have Fun and Disinfect. |
| `Entertainment` | `1` | Speed of fun and relaxing, such as Listen to Music, Dance, Massage, Look in Mirror and Admire. |
| `Exercise` | `1` | Speed of exercise and some chores, such as Work Out, Running, Do Push-ups, Manual Power Generation, Do Laundry, Clean and Take a Bath. |
| `Social` | `1` | Speed of quiet time, such as Look Outside, Listen to Radio, Meditate, Journal and Do Yoga. |
| `Move` | `1` | Moving around the map and between floors. These last as long as the walk, so this setting changes nothing; use `[Walking]` instead. |
| `Free` | `1` | No action in the game uses this type, so this setting changes nothing. |
| `RepairFurniture` | `1` | Speed of repairs and building work, such as Repair, Fix Door, Restore Power, Reinforce Door, Install Furniture, Move Furniture, Dismantle and Scrap. |
| `Reading` | `0` | Speed of reading books, magazines, newspapers, notes and letters, whatever their type. An action counts as reading when the game plays its reading animation or sound for it. `0` leaves reading at its type's speed. |
| `FullBenefits` | `true` | When true, a sped-up action still gives its full effect: a faster sleep rests you as much as a normal one. When false, a sped-up action ends early and gives only what it gave in that shorter time. |
| `Overrides` | empty | Speeds for single actions, written as `id:speed` and separated by commas, for example `112:4, 39:2`. An override beats its type's speed. `112:4` makes Use Toilet four times as fast. Every action and its id is listed in `BepInEx\action-speed-actions.txt`. |
| `FreeSecondSearch` | `true` | When true, searching a container you have already searched opens it at once: no game time passes, so on an expedition it adds no exposure. When false, every search takes the game's time (or its type's or override's speed). |

To find an action's id and type, open `BepInEx\action-speed-actions.txt`. The mod writes it once the game has loaded its tables. Each line reads `id | name | type | game clock -> with this mod | why`, with times in minutes of game time. When an action keeps its full benefits, its time ends in `, full benefits`. The last column is one of:

- `Changed`: sped up (or slowed down) by its type's speed.
- `Overridden`: set by `Overrides`.
- `Reading`: set by `Reading`.
- `GameSpeed`: its type's speed is 1, so it keeps the game's time.
- `TimedElsewhere`: not timed by this table (crafting, brewing, cooking and some others). Its time shows as "not timed by this table", and neither a type speed nor an override changes it. Use the `[Speed]` section for crafting. Brewing and cooking are in the Machine Speed mod.

`FreeSecondSearch` counts a container as searched once its contents have been opened the first time. It applies to every search that opens a container, whatever its type speed or override, with `FullBenefits` on or off. The action list does not show it, because it depends on the container.

Type speeds and override speeds are held between 0.25 and 20. A value below 0.25 is raised to 0.25 and a value above 20 is lowered to 20.

What 0 does here:

- A type speed of `0` (or a negative number) is raised to 0.25, which makes that type take four times as long. To leave a type at the game's pace, write `1`.
- An override with a speed of `0` or less, or an entry that is not `id:speed`, is skipped. The log says `ignored override '<entry>', write it as id:speed with a speed above 0` at startup and whenever an action setting changes.
- A `Reading` of `0` or less leaves every reading action at its type's speed. Above 0 it is held between 0.25 and 20 like the type speeds.
- An override of `1` puts that one action back at the game's pace even when its type is sped up.

## Walking

| Setting | Default | What it does |
| --- | --- | --- |
| `Speed` | `1.5` | How fast you walk, meaning the character you control. |
| `RobotSpeed` | `1.5` | How fast robots move. |
| `SurvivorSpeed` | `1.5` | How fast other people at your base walk. Enemies always keep the game's speed. |

1 is the game's speed and 2 is twice as fast. Each is held between 0.25 and 5. A value below 0.25 is raised to 0.25 and a value above 5 is lowered to 5. A `0` (or a negative number) is raised to 0.25, a quarter of the game's walking speed. When all three are exactly 1, the mod does not touch walking at all.

## Speed

Crafting is timed apart from the action table, so it has its own speed here. At 1 (the default) the mod does not touch crafting at all. Brewing and cooking speeds are in the Machine Speed mod.

| Setting | Default | What it does |
| --- | --- | --- |
| `Crafting` | `1` | Speed of crafting at a workbench. 2 halves the time a craft takes. |

It is held between 0.25 and 20. A value outside that range is pulled to the nearest end, and the log says so at startup and when the setting changes, for example `Speed/Crafting = 50 is outside 0.25 to 20, using 20`.

What 0 does here: `0` or a negative number counts as 1, the game's pace, and the log says so at startup and when the setting changes, for example `Speed/Crafting = 0 is not a speed above 0, using the game's pace (1)`.

A craft keeps the length it had when it started, so a change here only affects crafts started after it.
