# Action Speed settings

Settings live in `BepInEx\config\com.dev.survivallog.actionspeed.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back. Each setting in the file says what it does, in English and Chinese. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

A speed of 1 is always the game's own pace. 2 takes half the game time, 4 takes a quarter, 0.5 takes twice as long. What `0` does is different in each section, so it is noted under each table.

## Actions

Action speeds come in three levels. `Everything` in `[Actions]` sets every action at once. Each section below (`[Eating]`, `[Health]` and so on) has an `All` that sets the whole section. Each single setting in a section sets one kind of action. The most specific setting above 0 wins: a single setting beats its section's `All`, and `All` beats `Everything`. Leave a setting at `0` to follow the level above it.

Every action and the setting it follows are listed in `BepInEx\action-speed-actions.txt` (see below).

### [Actions]

| Setting | Default | What it does |
| --- | --- | --- |
| `Everything` | `1` | Speed of every action, unless a section's `All` or a single setting says otherwise. `0` or less counts as 1. |
| `Other` | `0` | Every action no other setting covers, such as Talk and switching electronics on and off. `0` follows `Everything`. |
| `FullBenefits` | `true` | When true, a sped-up action still gives its full effect: a faster sleep rests you as much as a normal one. When false, a sped-up action ends early and gives only what it gave in that shorter time. |
| `Overrides` | empty | Speeds for single actions, written as `id:speed` and separated by commas, for example `112:4, 39:2`. An override beats every speed setting. `112:4` makes Use Toilet four times as fast. |
| `FreeSecondSearch` | `false` | When true, searching a container you have already searched opens it at once: no game time passes, so on an expedition it adds no exposure. When false, every search takes the game's time (or its speed setting's or override's). |

### Sections

Every section has `All` (default `0`, follows `Everything`). Every single setting defaults to `0` and follows its section's `All`.

| Section | Setting | Covers |
| --- | --- | --- |
| `[Eating]` | `Meals` | Meals and ingredients, such as Eat Crackers, Eat Carrot and Eat Mushroom Soup. |
| | `Snacks` | Snacks and fruit, such as Eat Cookies, Eat Candy, Eat Banana and Eat Potato Chips. |
| | `Drinks` | Drinks, such as Drink Coffee, Drink Cola and Drink Whisky. |
| `[Health]` | `Medicine` | Medicine and first aid, such as Take Meds, Eat Vitamins, Use Bandage and Patch Yourself Up. |
| | `Sickness` | Vomiting and Diarrhea. |
| | `Moods` | Short moments like Hungry..., Tired..., Sad... and feeling cold. |
| `[Hygiene]` | `Toilet` | Use Toilet and the other toilet actions. |
| | `Washing` | Wash Hands, Take a Bath, Brush Teeth and other washing up. |
| `[Rest]` | `Sleep` | Sleep. |
| | `Nap` | Nap, Snooze... and other short rests. |
| `[Leisure]` | `Reading` | Books, magazines, newspapers, notes and letters, wherever they come from. An action counts as reading when the game plays its reading animation or sound for it. |
| | `Games` | Play Video Games, Play Darts, Use Playing Cards and trying out electronics. |
| | `Music` | Listen to Music, Listen to Radio and Put on Record. |
| | `Dancing` | Dance. |
| | `QuietTime` | Look Outside, Meditate, Journal and Do Yoga. |
| | `Relaxing` | Rest, Massage, Look in Mirror, Admire and View Artwork. |
| | `Workouts` | Work Out, Running, Do Push-ups and Use Dumbbells. |
| `[Chores]` | `Cleaning` | Clean, Disinfect and Do Laundry. |
| | `Organizing` | Organize Items (id 11), Put Away and Check Supplies. |
| | `BoilingWater` | Boil Water. |
| | `Power` | Manual Power Generation and Turn On Generator. |
| | `Unpacking` | Unpacking furniture packages and opening packs and gift boxes, such as Open Fruit Crate. |
| | `Salvaging` | Take Apart Chime, Break Trinket, Flatten Can and other collectibles taken apart. |
| `[Farming]` | `Planting` | Planting seeds and transplanting. |
| | `Watering` | Watering crops. |
| | `Harvesting` | Harvest. |
| | `Tending` | Remove Weeds, Remove Pests, tilling soil and using fertilizer. |
| `[Building]` | `Repairs` | Repair, Fix Door, Restore Power and Repair Stairs. |
| | `Reinforcing` | Reinforce Door, Reinforce Window and Upgrade Gate. |
| | `Furnishing` | Install Furniture and Move Furniture. |
| | `Demolishing` | Demolish, Dismantle and Scrap. |
| | `Projects` | Create Rescue Beacon, Unlock Furniture, studying a build and other building projects. |
| `[Scavenging]` | `Searching` | Search and taking supplies. |
| | `BreakingIn` | Unlock, Pry Open, Pick Lock and Force Open. |
| | `Inspecting` | View, Look Closely and Leaf Through. |
| | `Drones` | Drone Patrol and Drone Scavenging. |
| `[Defense]` | `Traps` | Set Trap, Install Trap, Dismantle Trap and Move Trap. |
| | `Throwing` | Throw Molotov Cocktail, Throw Glow Stick and Fire Flare. |

For example, to make everything twice as fast but leave sleep alone: `Everything = 2` in `[Actions]` and `Sleep = 1` in `[Rest]`. To speed up only farming: `All = 3` in `[Farming]`.

### The action list

To find an action's id and the setting it follows, open `BepInEx\action-speed-actions.txt`. The mod writes it once the game has loaded its tables, and again once the game's language has loaded. Each line reads `id | name | setting | game clock -> with this mod | why`, with times in minutes of game time. The setting is written as `Section/Setting`, such as `Hygiene/Toilet`. When an action keeps its full benefits, its time ends in `, full benefits`. The last column is one of:

- `Changed`: sped up (or slowed down) by its speed setting.
- `Overridden`: set by `Overrides`.
- `GameSpeed`: its speed works out to 1, so it keeps the game's time.
- `TimedElsewhere`: not timed by this table (crafting, brewing, cooking and some others). Its time shows as "not timed by this table", and neither a speed setting nor an override changes it. Use the `[Speed]` section for crafting. Brewing and cooking are in the Machine Speed mod.

`FreeSecondSearch` counts a container as searched once its contents have been opened the first time. It applies to every search that opens a container, whatever its speed or override, with `FullBenefits` on or off. The action list does not show it, because it depends on the container.

Speeds and override speeds are held between 0.25 and 20. A value below 0.25 is raised to 0.25 and a value above 20 is lowered to 20.

What 0 does here:

- `Everything` at `0` or less counts as 1, the game's pace.
- An `All` or a single setting at `0` or less follows the level above it. To put one kind of action back at the game's pace while the rest is sped up, write `1`.
- An override with a speed of `0` or less, or an entry that is not `id:speed`, is skipped. The log says `ignored override '<entry>', write it as id:speed with a speed above 0` at startup and whenever an action setting changes.
- An override of `1` puts that one action back at the game's pace even when its setting is sped up.

### Settings from 0.7 and older

Older versions had one speed per game action type (`FurnitureUse`, `Food`, `Rest`, `Entertainment`, `Exercise`, `Social`, `Treatment`, `Move`, `Other`, `Free`, `RepairFurniture`) and a `Reading` speed. The first time 0.8 starts with such a file, it moves those values to the new settings so every action keeps the speed it had, then removes the old keys. The speed most actions shared becomes `Everything`, a section where most settings share a speed gets that as its `All`, and only the settings that differ get their own value. The log says what was moved. The old `[Speed]` `Brewing` and `Cooking` lines, which Machine Speed took over in 0.5, are removed too.

## Walking

| Setting | Default | What it does |
| --- | --- | --- |
| `Speed` | `1` | How fast you walk, meaning the character you control. |
| `RobotSpeed` | `1` | How fast robots move. |
| `SurvivorSpeed` | `1` | How fast other people at your base walk. Enemies always keep the game's speed. |

1 is the game's speed and 2 is twice as fast. Each is held between 0.25 and 5. A value below 0.25 is raised to 0.25 and a value above 5 is lowered to 5. A `0` (or a negative number) is raised to 0.25, a quarter of the game's walking speed. When all three are exactly 1, the mod does not touch walking at all.

## Work

| Setting | Default | What it does |
| --- | --- | --- |
| `Speed` | `1` | How fast you do every action, meaning the character you control. |
| `RobotSpeed` | `1` | How fast robots do every action. |
| `SurvivorSpeed` | `1` | How fast other people at your base do every action. |

A work speed adds to each action's own speed: with `Sleep` at 2 and your `Speed` at 2, you sleep four times as fast. The action still gives its full effect, even with `FullBenefits` off. Raiders and creatures always keep the game's pace. Each is held between 0.25 and 20, and `0` or a negative number counts as 1. When all three are exactly 1, they change nothing.

## Speed

Crafting and expedition travel are timed apart from the action table, so they have their own speeds here. At 1 (the default) the mod does not touch them at all. Brewing and cooking speeds are in the Machine Speed mod.

| Setting | Default | What it does |
| --- | --- | --- |
| `Crafting` | `1` | Speed of crafting at a workbench. 2 halves the time a craft takes. |
| `Travel` | `1` | Speed of the trip between home and an expedition place, going out and coming home. 2 halves the trip. The map shows the new time. A vehicle's discount still applies on top. |

Each is held between 0.25 and 20. A value outside that range is pulled to the nearest end, and the log says so at startup and when the setting changes, for example `Speed/Crafting = 50 is outside 0.25 to 20, using 20`.

What 0 does here: `0` or a negative number counts as 1, the game's pace, and the log says so at startup and when the setting changes, for example `Speed/Crafting = 0 is not a speed above 0, using the game's pace (1)`.

A craft keeps the length it had when it started, so a change here only affects crafts started after it. A `Travel` change applies to the next trip.
