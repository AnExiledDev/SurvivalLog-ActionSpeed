Name: Action Speed
Summary: Make eating, sleeping, reading, chores, farming, crafting and every other action take less game time while still giving the full benefit, so a nap twice as fast leaves you just as rested. Set one speed for everything, per section, or for each of 38 actions, plus work and walking speed for you, the robot and survivors, and expedition travel time.

Description (BBCode):
Short chores eat your day in Survival Log. Action Speed lets you choose how much game time each kind of action takes, and how fast you, the robot and other people at your base walk. Everything is set in one config file, and each setting there says what it covers.

[b]New install? Nothing changes until you set it.[/b] Every setting starts at the game's own pace, so a fresh install plays exactly like the base game. Before you start playing, raise the speeds you want: on the Mod Manager's Mods screen, or in [i]BepInEx\config\com.dev.survivallog.actionspeed.cfg[/i] while the game is closed (the file appears after the game has started once with the mod). Already have a config file from an older version? Its speeds move to the new settings by themselves, so every action keeps the speed it had.

[size=5]Main features[/size]
[list]
[*][b]A speed for each kind of action[/b]: 38 settings such as meals, snacks, sleep, naps, toilet, cleaning, harvesting, repairs, searching and traps. Each of the ten sections (eating, health, hygiene, rest, leisure, chores, farming, building, scavenging, defense) has one setting for the whole section, and one setting speeds up everything. Raise only what you want faster.[/*]
[*][b]Full benefits[/b]: a sped-up action still gives everything it gives at the game's pace. Sleep twice as fast and you wake up just as rested. You can turn this off to cut sped-up actions short instead.[/*]
[*][b]Free second search[/b]: searching a container you have already searched opens it at once, so it costs no time and, on an expedition, no exposure. Off by default.[/*]
[*][b]Reading[/b]: one speed for every book, magazine, newspaper, note and letter, wherever the game files them.[/*]
[*][b]Single actions[/b]: set a speed for one action by its id.[/*]
[*][b]Action list[/b]: every action with the setting it follows and its old and new time is written to a text file.[/*]
[*][b]Faster walking[/b]: you, the robot and your survivors each have their own walking speed, the game's speed by default. Enemies keep the game's speed.[/*]
[*][b]Work speed[/b]: you, the robot and your survivors can each work faster at everything they do, on top of each action's speed, still with the full effect. The game's pace by default.[/*]
[*][b]Expedition travel[/b]: shorter or longer trips to expedition places and back. The map shows the new time. Off by default.[/*]
[*][b]Crafting[/b]: its own speed. Off by default. Brewing and cooking speeds are in Machine Speed.[/*]
[/list]
Action Speed writes no data of its own into your save and does not edit game files. It does change how much game time things take, and your save reflects that. You can remove it at any time; actions started after that take the game's own time again.

Every setting in the config file is explained in English and Chinese.

[size=5]Known issues[/size]
Action speeds apply to everyone doing an action, survivors and the robot included. The bar over a character's head fills with a sped-up action, but other times the game shows may still be the full length while the action itself ends sooner.

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19611. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ActionSpeed.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]ActionSpeed.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To change settings, edit [font=Courier New]BepInEx\config\com.dev.survivallog.actionspeed.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once (see [url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/blob/main/CONFIG.md]CONFIG.md[/url] for every setting). Action ids are listed in [font=Courier New]BepInEx\action-speed-actions.txt[/font]. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\action-speed.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed]github.com/AnExiledDev/SurvivalLog-ActionSpeed[/url]

[line]
[size=5]简体中文[/size]

在 Survival Log 里，零碎的小事会把一天的时间吃光。Action Speed 让你自己决定每类动作要花多少游戏时间，以及你、机器人和基地里其他人走路有多快。所有设置都在一个配置文件里，每项设置都写明了它管哪些动作。

[b]新安装？不设置就不会有任何变化。[/b]所有设置默认都是游戏原速，所以刚装好时和原版游戏完全一样。开始游玩前，请先调高你想加快的速度：可以在 Mod Manager 的「模组」界面里改，也可以在游戏关闭时编辑 [i]BepInEx\config\com.dev.survivallog.actionspeed.cfg[/i]（带着本模组启动过一次游戏后才会生成这个文件）。已经有旧版本的配置文件？里面的速度会自动转到新设置，每个动作保持原来的速度。

[size=5]主要功能[/size]
[list]
[*][b]每种动作单独调速[/b]：共 38 项，例如正餐、零食、睡觉、打盹、如厕、打扫、收获、修理、搜索和陷阱。十个分类（吃喝、健康、卫生、休息、休闲、家务、种植、建造、搜刮、防御）各有一项统一设置，另有一项设置可以加快所有动作。只需调快你想加快的。[/*]
[*][b]效果不减[/b]：加速后的动作仍有和原速时一样的全部效果。睡觉快一倍，醒来照样休息得一样足。也可以关掉此项，让加速的动作直接提前结束。[/*]
[*][b]搜过的不再花时间[/b]：再次搜寻已经搜过的容器会立即打开，不花时间，外出探索时也不会增加暴露值。默认关闭。[/*]
[*][b]阅读[/b]：所有书籍、杂志、报纸、笔记和信件共用一个速度，不论游戏把它们归在哪里。[/*]
[*][b]单个动作[/b]：按 id 给某一个动作单独设置速度。[/*]
[*][b]动作列表[/b]：每个动作所属的设置以及原来和现在的时长都会写进一个文本文件。[/*]
[*][b]走得更快[/b]：你、机器人和幸存者各有自己的行走速度，默认为游戏原速。敌人保持游戏原速。[/*]
[*][b]工作速度[/b]：你、机器人和幸存者可以各自加快所做的每个动作，叠加在各动作自己的速度之上，效果仍然完整。默认为游戏原速。[/*]
[*][b]外出路程[/b]：缩短或延长前往外出地点和返回的路程。地图上显示新的时间。默认不改动。[/*]
[*][b]制造[/b]：有独立的速度，默认不改动。酿酒和烹饪的速度在 Machine Speed 模组中。[/*]
[/list]
Action Speed 不会往存档里写入自己的数据，也不会修改游戏文件。但它确实改变了各种事情花费的游戏时间，存档也会反映这一点。随时可以移除；移除后新开始的动作会恢复游戏原本的时长。

配置文件中的每项设置都有英文和中文说明。

[size=5]已知问题[/size]
动作速度对所有执行该动作的人都生效，包括幸存者和机器人。角色头顶的进度条会随加速的动作一起走完，但游戏在别处显示的时间可能仍是原本的完整时长，而动作本身会更早结束。工作台的进度条可能显示游戏原本的时间，而实际所需时间更短。

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19611。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效；没装的话，改动在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（改动设置无需重启游戏）：用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ActionSpeed.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]ActionSpeed.dll[/font]。

[size=5]配置[/size]
如需修改设置，可以在游戏内 Mod Manager 的「模组」界面修改（改动立即生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.actionspeed.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。动作的 id 列在 [font=Courier New]BepInEx\action-speed-actions.txt[/font] 中。没装 Exiled's Toolkit 的话，在「模组」界面的改动改为在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\action-speed.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-ActionSpeed]github.com/AnExiledDev/SurvivalLog-ActionSpeed[/url]
