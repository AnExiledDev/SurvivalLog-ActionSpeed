# Changelog

## 0.7.1

- After a game update, a hook that can no longer be placed is logged as an error, so the Mod Manager can mark the mod as maybe needing an update.
- Made for Survival Log 1.1.19131.

## 0.7.0

- New `Reading` setting: one speed for reading books, magazines, newspapers, notes and letters, whatever their action type. It is 0 (off) by default, so reading keeps its type's speed until you set it. An override for a single action still beats it.
- Every setting now starts at the game's own behavior, so nothing changes until you raise what you want: furniture use, food, treatment and other speeds go from 2 to 1, all three walking speeds from 1.5 to 1, and `FreeSecondSearch` from on to off. A config file you already have keeps its values; delete it to get the new defaults.
- Marked for game version 1.1.19131.

## 0.6.0

- The Mods screen links this changelog.
- Settings changed on the Mod Manager's Mods screen take effect at once. An action, walk or craft already under way keeps the length it started with.
- The Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings changed on the Mods screen take effect without a restart; without it, at the next game start.

## 0.5.0

The `Brewing` and `Cooking` settings moved to the new Machine Speed mod, which reads your values from this mod's config the first time it starts. Action Speed now covers what you do; Machine Speed covers how fast machines work.

## 0.4.2

The workbench progress bar fills all the way when crafting is sped up. Before, the craft finished while the bar was still at about a fifth. No setting changed.

## 0.4.1

First public release. Makes everyday actions like eating, using furniture and treatment take less game time without giving less, lets you, the robot and your survivors walk faster, and can speed up crafting, brewing and cooking.
