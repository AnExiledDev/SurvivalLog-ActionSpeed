using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ActionSpeed.Core;
using BepInEx.Configuration;

namespace ActionSpeed.Adapters;

/// <summary>
/// The action speed settings: one everything speed, an umbrella speed per section and a speed per group, where 0
/// follows the speed above. Also moves a settings file written by an older version, which had one speed per game
/// action type, onto these.
/// </summary>
internal sealed class ActionSettings
{
    public const string AllKey = "All";

    private const string Range = "From 0.25 to 20: 1 is the game's pace, 2 is twice as fast.";
    private const string RangeChinese = "范围 0.25 到 20：1 为游戏原速，2 为两倍速。";

    /// <summary>[Speed] keys from versions that timed brewing and cooking, which the Machine Speed mod does now.</summary>
    private static readonly string[] RetiredSpeedKeys = { "Brewing", "Cooking" };

    private static readonly Dictionary<string, (string English, string Chinese)> SectionCovers = new()
    {
        ["Eating"] = ("eating and drinking", "吃喝"),
        ["Health"] = ("medicine, sickness and passing moods", "用药、生病和一时的情绪"),
        ["Hygiene"] = ("the toilet and washing", "如厕和洗漱"),
        ["Rest"] = ("sleeping and napping", "睡觉和打盹"),
        ["Leisure"] = ("reading, games, music, dancing, quiet time, relaxing and working out", "阅读、游戏、音乐、跳舞、独处、放松和锻炼"),
        ["Chores"] = ("cleaning, organizing, boiling water, power, unpacking and salvaging", "打扫、整理、烧水、发电、拆封和回收"),
        ["Farming"] = ("planting, watering, harvesting and tending crops", "播种、浇水、收获和照料作物"),
        ["Building"] = ("repairs, reinforcing, furniture, demolishing and building projects", "修理、加固、家具、拆除和建造项目"),
        ["Scavenging"] = ("searching, breaking in, inspecting and drones", "搜索、破门开锁、查看和无人机"),
        ["Defense"] = ("traps and throwing", "陷阱和投掷"),
    };

    private static readonly Dictionary<Group, (string English, string Chinese)> GroupCovers = new()
    {
        [Group.Meals] = ("Meals and ingredients, such as Eat Crackers, Eat Carrot and Eat Mushroom Soup.", "正餐和食材，例如吃苏打饼干、品尝胡萝卜、吃蘑菇汤。"),
        [Group.Snacks] = ("Snacks and fruit, such as Eat Cookies, Eat Candy, Eat Banana and Eat Potato Chips.", "零食和水果，例如吃饼干、吃糖、品尝香蕉、吃薯片。"),
        [Group.Drinks] = ("Drinks, such as Drink Coffee, Drink Cola and Drink Whisky.", "饮品，例如品尝咖啡、品尝可乐、喝威士忌。"),
        [Group.Medicine] = ("Medicine and first aid, such as Take Meds, Eat Vitamins, Use Bandage and Patch Yourself Up.", "用药和急救，例如吃治疗药、吃维生素、使用绷带、治愈自己。"),
        [Group.Sickness] = ("Being sick: Vomiting and Diarrhea.", "生病：呕吐、腹泻。"),
        [Group.Moods] = ("Short moments like Hungry..., Tired..., Sad... and feeling cold.", "短暂的情绪反应，例如「好饿！」「好困！」「好低落！」和觉得冷。"),
        [Group.Toilet] = ("Use Toilet and the other toilet actions.", "上厕所（坐下就开始……）和其他如厕动作。"),
        [Group.Washing] = ("Washing up, such as Wash Hands, Take a Bath and Brush Teeth.", "洗漱，例如洗手、洗澡、刷牙。"),
        [Group.Sleep] = ("Sleep.", "睡觉。"),
        [Group.Nap] = ("Naps and short rests, such as Nap and Snooze....", "打盹和小憩，例如打盹、小睡一会。"),
        [Group.Reading] = ("Reading books, magazines, newspapers, notes and letters, wherever they come from.", "阅读书籍、杂志、报纸、笔记和信件，不论来自哪里。"),
        [Group.Games] = ("Games, such as Play Video Games, Play Darts, Use Playing Cards and trying out electronics.", "游戏，例如玩电视游戏、玩下飞镖、使用纸牌，以及试玩电器。"),
        [Group.Music] = ("Music, such as Listen to Music, Listen to Radio and Put on Record.", "音乐，例如听音乐、听收音机、放唱片。"),
        [Group.Dancing] = ("Dance.", "跳舞。"),
        [Group.QuietTime] = ("Quiet time, such as Look Outside, Meditate, Journal and Do Yoga.", "独处时光，例如观察窗外、冥想、写日记、瑜伽。"),
        [Group.Relaxing] = ("Relaxing, such as Rest, Massage, Look in Mirror, Admire and View Artwork.", "放松，例如休息、按摩、照镜子、鉴赏、欣赏画作。"),
        [Group.Workouts] = ("Exercise, such as Work Out, Running, Do Push-ups and Use Dumbbells.", "运动，例如锻炼、跑步、做俯卧撑、使用哑铃。"),
        [Group.Cleaning] = ("Cleaning, such as Clean, Disinfect and Do Laundry.", "打扫，例如打扫卫生、消毒、洗衣服。"),
        [Group.Organizing] = ("Organizing, such as Organize Items, Put Away and Check Supplies.", "整理，例如整理物品、收起来、清点物资。"),
        [Group.BoilingWater] = ("Boil Water.", "烧水。"),
        [Group.Power] = ("Power, such as Manual Power Generation and Turn On Generator.", "电力，例如手动发电、开启发电机。"),
        [Group.Unpacking] = ("Unpacking furniture packages and opening packs and gift boxes, such as Open Fruit Crate.", "拆封家具包裹，打开物资包和礼盒，例如打开水果箱。"),
        [Group.Salvaging] = ("Salvaging collectibles, such as Take Apart Chime, Break Trinket and Flatten Can.", "回收收藏品，例如拆开风铃、敲碎摆件、压扁易拉罐。"),
        [Group.Planting] = ("Planting seeds and transplanting.", "播种和移植。"),
        [Group.Watering] = ("Watering crops.", "浇水。"),
        [Group.Harvesting] = ("Harvest.", "收获。"),
        [Group.Tending] = ("Tending crops, such as Remove Weeds, Remove Pests, tilling soil and using fertilizer.", "照料作物，例如除草、除虫、翻土和施肥。"),
        [Group.Repairs] = ("Repairs, such as Repair, Fix Door, Restore Power and Repair Stairs.", "修理，例如修理设施、修补房门、修复电力、修楼梯。"),
        [Group.Reinforcing] = ("Reinforcing, such as Reinforce Door, Reinforce Window and Upgrade Gate.", "加固，例如加固房门、加固窗户、升级大门。"),
        [Group.Furnishing] = ("Install Furniture and Move Furniture.", "安装家具、移动家具。"),
        [Group.Demolishing] = ("Demolish, Dismantle and Scrap.", "拆除设施和回收。"),
        [Group.Projects] = ("Building projects, such as Create Rescue Beacon, Unlock Furniture and studying a build.", "建造项目，例如制作救援信标、解锁家具、研究构造。"),
        [Group.Searching] = ("Search and taking supplies.", "搜索和拿取物资。"),
        [Group.BreakingIn] = ("Getting through locks and doors, such as Unlock, Pry Open, Pick Lock and Force Open.", "开锁破门，例如解锁、强行撬开、撬锁、强行打开。"),
        [Group.Inspecting] = ("Looking things over, such as View, Look Closely and Leaf Through.", "查看物品，例如查看、仔细看看、翻阅。"),
        [Group.Drones] = ("Drone work, such as Drone Patrol and Drone Scavenging.", "无人机任务，例如无人机巡逻、无人机搜刮。"),
        [Group.Traps] = ("Set Trap, Install Trap, Dismantle Trap and Move Trap.", "设置、安装、拆除和移动陷阱。"),
        [Group.Throwing] = ("Throwing, such as Throw Molotov Cocktail, Throw Glow Stick and Fire Flare.", "投掷，例如投掷燃烧瓶、扔荧光棒、发射信号弹。"),
        [Group.Other] = ("Every action no other setting covers, such as Talk and switching electronics on and off.", "其他设置都不包含的动作，例如交谈、开关电器。"),
    };

    private readonly ConfigEntry<float> _everything;
    private readonly IReadOnlyDictionary<string, ConfigEntry<float>> _sections;
    private readonly IReadOnlyDictionary<Group, ConfigEntry<float>> _groups;

    private ActionSettings(ConfigEntry<float> everything, IReadOnlyDictionary<string, ConfigEntry<float>> sections, IReadOnlyDictionary<Group, ConfigEntry<float>> groups)
    {
        _everything = everything;
        _sections = sections;
        _groups = groups;
    }

    public IEnumerable<ConfigEntryBase> All => _sections.Values.Concat(_groups.Values).Prepend(_everything);

    public static ActionSettings Bind(ConfigFile config)
    {
        var everything = config.Bind(ActionGroups.ActionsSection, LegacySettings.EverythingKey, 1f,
            $"Speed of every action, unless a section's All or a single setting below says otherwise. {Range} 0 or less counts as 1.\n" +
            $"所有动作的速度，除非某个分类的 All 或下面的单项另有设置。{RangeChinese}0 或更小按 1 计算。");
        var sections = ActionGroups.Sections.ToDictionary(section => section.Name, section => BindSection(config, section.Name));
        var groups = Enum.GetValues<Group>().ToDictionary(group => group, group => BindGroup(config, group));

        return new ActionSettings(everything, sections, groups);
    }

    public IReadOnlyDictionary<Group, float> Speeds() =>
        GroupSpeeds.Resolve(new ConfiguredSpeeds(_everything.Value,
            _sections.ToDictionary(pair => pair.Key, pair => pair.Value.Value),
            _groups.ToDictionary(pair => pair.Key, pair => pair.Value.Value)));

    /// <summary>
    /// Moves a file written by an older version onto the group settings, then removes the old keys. Runs before any
    /// setting is applied. A file that cannot be read or tidied keeps working: the old keys are only left behind.
    /// </summary>
    public void MoveOldSettings(string? savedSettings, ConfigFile config, Action<string> log)
    {
        var oldActions = savedSettings != null && LegacySettings.Find(savedSettings) is { } found ? new Dictionary<string, string>(found) : null;
        var wasSaving = config.SaveOnConfigSet;

        config.SaveOnConfigSet = false;

        try
        {
            if (oldActions != null) Apply(LegacySettings.Migrate(oldActions), log);

            var removed = RemoveRetiredKeys(config, log);

            if (oldActions != null || removed > 0) config.Save();
        }
        finally
        {
            config.SaveOnConfigSet = wasSaving;
        }
    }

    private void Apply(Migration migration, Action<string> log)
    {
        _everything.Value = migration.Speeds.Everything;

        foreach (var (name, speed) in migration.Speeds.Sections) _sections[name].Value = speed;

        foreach (var (group, speed) in migration.Speeds.Groups) _groups[group].Value = speed;

        var changed = migration.Speeds.Sections.Where(pair => pair.Value > 0f).Select(pair => $"{pair.Key}/{AllKey} x{pair.Value:0.##}")
            .Concat(migration.Speeds.Groups.Where(pair => pair.Value > 0f).Select(pair => $"{ActionGroups.Label(pair.Key)} x{pair.Value:0.##}"));

        log($"moved the old action type speeds to the new settings: Everything x{migration.Speeds.Everything:0.##}, {string.Join(", ", changed.DefaultIfEmpty("nothing else set"))}");
    }

    /// <summary>The settings file as it was before this version bound its keys, or null when there is none yet.</summary>
    public static string? ReadFile(string path, Action<string> log)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex)
        {
            log($"could not read the settings file to move old settings: {ex.Message}");

            return null;
        }
    }

    /// <summary>
    /// BepInEx keeps keys no plugin binds and writes them back on every save, and only offers them through a private
    /// property, so they are removed through it; if a BepInEx update renames it, the keys just stay in the file.
    /// </summary>
    private static int RemoveRetiredKeys(ConfigFile config, Action<string> log)
    {
        try
        {
            var orphans = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(config) as IDictionary;

            if (orphans == null) return 0;

            var retired = orphans.Keys.OfType<ConfigDefinition>().Where(IsRetired).ToList();

            foreach (var key in retired) orphans.Remove(key);

            return retired.Count;
        }
        catch (Exception ex)
        {
            log($"could not remove old settings from the file: {ex.Message}");

            return 0;
        }
    }

    private static bool IsRetired(ConfigDefinition key) =>
        key.Section == ActionGroups.ActionsSection && LegacySettings.IsRetiredKey(key.Key) || key.Section == "Speed" && RetiredSpeedKeys.Contains(key.Key);

    private static ConfigEntry<float> BindSection(ConfigFile config, string section)
    {
        var (english, chinese) = SectionCovers[section];

        return config.Bind(section, AllKey, 0f,
            $"Speed of {english}, unless a single setting below says otherwise. {Range} 0 or less follows Everything in [Actions].\n" +
            $"{chinese}的速度，除非下面的单项另有设置。{RangeChinese}0 或更小表示跟随 [Actions] 的 Everything。");
    }

    private static ConfigEntry<float> BindGroup(ConfigFile config, Group group)
    {
        var (english, chinese) = GroupCovers[group];
        var section = ActionGroups.SectionOf(group);
        var follows = section == ActionGroups.ActionsSection ? "Everything" : $"{AllKey} in this section";
        var followsChinese = section == ActionGroups.ActionsSection ? "Everything" : $"本分类的 {AllKey}";

        return config.Bind(section, group.ToString(), 0f,
            $"{english} {Range} 0 or less follows {follows}.\n" +
            $"{chinese}{RangeChinese}0 或更小表示跟随{followsChinese}。");
    }
}
