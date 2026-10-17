Name: Action Speed
Summary: Makes everyday actions like eating, using furniture and treatment take less game time without giving less, lets you, the robot and your survivors walk faster, and can speed up crafting, brewing and cooking. 让吃饭、使用家具、吃药等日常动作花更少的游戏时间而效果不减，让你、机器人和幸存者走得更快，还能加快制造、酿酒和烹饪。

Description (BBCode):
Short chores eat your day in Survival Log. Action Speed lets you choose how much game time each kind of action takes, and how fast you, the robot and other people at your base walk. Everything is set in one config file, and each setting there says what it covers.

[size=5]Main features[/size]
[list]
[*][b]Speed per action type[/b]: furniture use, food, treatment and other actions take half the game time by default. Every other type can be set too.[/*]
[*][b]Full benefits[/b]: a sped-up action still gives everything it gives at the game's pace. Sleep twice as fast and you wake up just as rested. You can turn this off to cut sped-up actions short instead.[/*]
[*][b]Free second search[/b]: searching a container you have already searched opens it at once, so it costs no time and, on an expedition, no exposure.[/*]
[*][b]Single actions[/b]: set a speed for one action by its id.[/*]
[*][b]Action list[/b]: every action with its old and new time is written to a text file.[/*]
[*][b]Faster walking[/b]: you, the robot and your survivors each have a walking speed, 1.5 times as fast by default. Enemies keep the game's speed.[/*]
[*][b]Crafting, brewing and cooking[/b]: each has its own speed. Off by default.[/*]
[/list]
Action Speed writes no data of its own into your save and does not edit game files. It does change how much game time things take, and your save reflects that. You can remove it at any time; actions started after that take the game's own time again.

Every setting in the config file is explained in English and Chinese.

[size=5]Known issues[/size]
Speeds change game time, and game time is finicky: events, sleep and zombie waves all run on it, so odd timing is worth reporting. Full benefits, the free second search, the robot and survivor walking speeds, and the crafting, brewing and cooking speeds have not been run in the game yet. Back up your save before trying it for the first time. Action speeds apply to everyone doing an action, survivors and the robot included. The bar over a character's head fills with a sped-up action, but other times the game shows may still be the full length while the action itself ends sooner. The workbench progress bar and the cooking bar can show the game's time while the work itself is shorter. If you run into anything strange, please tell me (see Feedback below).

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.18902. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Put [font=Courier New]ActionSpeed.dll[/font] in the BepInEx\plugins folder inside the game folder. Full path example:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ActionSpeed.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]ActionSpeed.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To change settings, edit [font=Courier New]BepInEx\config\com.dev.survivallog.actionspeed.cfg[/font] while the game is closed (see [url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/blob/main/CONFIG.md]CONFIG.md[/url] for every setting). Changes apply at the next game start. Action ids are listed in [font=Courier New]BepInEx\action-speed-actions.txt[/font].

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or leave a comment here. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\action-speed.log[/font].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed]github.com/AnExiledDev/SurvivalLog-ActionSpeed[/url]

[line]
[size=5]简体中文[/size]

在 Survival Log 里，零碎的小事会把一天的时间吃光。Action Speed 让你自己决定每类动作要花多少游戏时间，以及你、机器人和基地里其他人走路有多快。所有设置都在一个配置文件里，每项设置都写明了它管哪些动作。

[size=5]主要功能[/size]
[list]
[*][b]按动作类型调速[/b]：家具类、吃喝、吃药和其他动作默认只花一半的游戏时间。其余类型也都可以单独设置。[/*]
[*][b]效果不减[/b]：加速后的动作仍有和原速时一样的全部效果。睡觉快一倍，醒来照样休息得一样足。也可以关掉此项，让加速的动作直接提前结束。[/*]
[*][b]搜过的不再花时间[/b]：再次搜寻已经搜过的容器会立即打开，不花时间，外出探索时也不会增加暴露值。[/*]
[*][b]单个动作[/b]：按 id 给某一个动作单独设置速度。[/*]
[*][b]动作列表[/b]：每个动作原来和现在的时长都会写进一个文本文件。[/*]
[*][b]走得更快[/b]：你、机器人和幸存者各有自己的行走速度，默认是原来的 1.5 倍。敌人保持游戏原速。[/*]
[*][b]制造、酿酒和烹饪[/b]：各有独立的速度，默认不改动。[/*]
[/list]
Action Speed 不会往存档里写入自己的数据，也不会修改游戏文件。但它确实改变了各种事情花费的游戏时间，存档也会反映这一点。随时可以移除；移除后新开始的动作会恢复游戏原本的时长。

配置文件中的每项设置都有英文和中文说明。

[size=5]已知问题[/size]
速度改变的是游戏时间，而游戏时间牵一发而动全身：事件、睡眠和尸潮都依赖它，所以时间上有任何异常都值得反馈。效果不减、再次搜寻、机器人和幸存者的行走速度，以及制造、酿酒和烹饪的速度还没有在游戏中实际跑过。第一次使用前请先备份存档。动作速度对所有执行该动作的人都生效，包括幸存者和机器人。角色头顶的进度条会随加速的动作一起走完，但游戏在别处显示的时间可能仍是原本的完整时长，而动作本身会更早结束。工作台的进度条和烹饪的进度条可能显示游戏原本的时间，而实际所需时间更短。遇到任何异常，欢迎告诉我（见下方「反馈」）。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.18902。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]把 [font=Courier New]ActionSpeed.dll[/font] 放进游戏目录下的 BepInEx\plugins 文件夹。完整路径示例：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ActionSpeed.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]ActionSpeed.dll[/font]。

[size=5]配置[/size]
如需修改设置，请在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.actionspeed.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。修改会在下次启动游戏时生效。动作的 id 列在 [font=Courier New]BepInEx\action-speed-actions.txt[/font] 中。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在这里留言。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\action-speed.log[/font]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed]github.com/AnExiledDev/SurvivalLog-ActionSpeed[/url]
