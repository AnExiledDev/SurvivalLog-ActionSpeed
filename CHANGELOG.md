# Changelog

## 0.9.0

- New `[Work]` settings: `Speed` for you, `RobotSpeed` for robots and `SurvivorSpeed` for other people at your base. Each speeds up every action that character does, on top of the action's own speed, and the action still gives its full effect. All three start at 1, the game's pace.
- New `[Speed]` `Travel` setting: how long trips between home and an expedition place take, going out and coming home. The map shows the new time. It starts at 1, the game's pace.

## 0.8.0

- Action speeds are now set per kind of action instead of per game action type: 38 settings such as `Meals`, `Snacks`, `Drinks`, `Sleep`, `Nap`, `Reading`, `Toilet`, `Cleaning`, `Harvesting`, `Repairs`, `Searching` and `Traps`, in ten sections. Each section has an `All` setting for the whole section, and `Everything` in `[Actions]` sets every action at once. A setting at 0 follows the one above it.
- Your old settings move over by themselves the first time the game starts: every action keeps the speed it had, and the old keys are removed from the config file. The log says what was moved.
- The useless `Move` and `Free` settings are gone; those actions are timed by walking or not timed at all.
- The old `Brewing` and `Cooking` lines left in `[Speed]` since 0.5 are removed from the config file.
- The action list shows action names in the game's language. Before, names could show as raw text keys when the list was written before the game's language had loaded.
- The action list shows the setting each action follows, such as `Hygiene/Toilet`.

## 0.7.1

- After a game update, Mod Manager can tell you if this mod may need an update.
- Made for Survival Log 1.1.19131.

## 0.7.0

- New `Reading` setting: one speed for reading books, magazines, newspapers, notes and letters, whatever their action type. It is 0 (off) by default, so reading keeps its type's speed until you set it. An override for a single action still beats it.
- Every setting now starts at the game's own behavior, so nothing changes until you raise what you want: furniture use, food, treatment and other speeds go from 2 to 1, all three walking speeds from 1.5 to 1, and `FreeSecondSearch` from on to off. A config file you already have keeps its values; delete it to get the new defaults.
- Marked for game version 1.1.19131.

## 0.6.0

- Mod Manager's Mods screen links to this changelog.
- Settings changed on the Mod Manager's Mods screen take effect at once. An action, walk or craft already under way keeps the length it started with.
- Mod Manager's Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings you change on the Mods screen take effect right away. Without it, they take effect the next time you start the game.

## 0.5.0

- The `Brewing` and `Cooking` settings moved to the new Machine Speed mod, which reads your values from this mod's config the first time it starts.
- Action Speed now covers what you do, and Machine Speed covers how fast machines work.

## 0.4.2

- The workbench progress bar fills all the way when crafting is sped up. Before, the craft finished while the bar was still at about a fifth.

## 0.4.1

- First release.
- Everyday actions like eating, using furniture and treatment take less game time, and still give as much.
- You, the robot and your survivors can walk faster.
- Crafting, brewing and cooking can be sped up.
