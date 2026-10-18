using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionSpeed.Adapters;
using ActionSpeed.Core;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using GameCore.HotUpdate;
using HarmonyLib;

namespace ActionSpeed;

[BepInPlugin(Guid, "Action Speed", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.actionspeed";
    private const string Version = "0.4.2";
    private const string FurnitureUse = "FurnitureUse";

    private const string ActionRange = "From 0.25 to 20: 1 is the game's pace, 2 is twice as fast.";
    private const string ActionRangeChinese = "范围 0.25 到 20：1 为游戏原速，2 为两倍速。";
    private const string WalkRange = "From 0.25 to 5: 1 is the game's speed, 2 is twice as fast.";
    private const string WalkRangeChinese = "范围 0.25 到 5：1 为游戏原速，2 为两倍速。";

    /// <summary>Short chores players ask to speed up; sleep, fun, workouts, quiet time and repairs keep the game's pace until set.</summary>
    private static readonly Dictionary<string, float> DefaultSpeeds = new() { [FurnitureUse] = 2f, ["Food"] = 2f, ["Treatment"] = 2f, ["Other"] = 2f };

    /// <summary>What each action type covers, in the game's own action names, so the player can tell them apart.</summary>
    private static readonly Dictionary<string, (string English, string Chinese)> TypeCovers = new()
    {
        [FurnitureUse] = (
            "Speed of furniture actions, such as unpacking furniture packages, Take Apart Chime, Break Trinket, Deploy Rescue Beacon, Upgrade Gate and Try Cooking.",
            "家具类动作的速度，例如拆封家具包裹、拆开风铃、敲碎摆件、架设救援信标、升级大门、尝试烹饪。"),
        ["Food"] = (
            "Speed of eating and drinking, such as Eat Cookies, Eat Burger, Drink Coffee, Enjoy Hot Pot and Eat Food.",
            "吃喝的速度，例如吃饼干、吃汉堡、品尝咖啡、享用火锅、好饿，吃点东西。"),
        ["Rest"] = (
            "Speed of sleeping and resting, such as Sleep, Nap, Play Video Games, Have Fun and Disinfect.",
            "睡觉和休息的速度，例如睡觉、打盹、小睡一会、玩游戏、无聊，娱乐一下、消毒。"),
        ["Entertainment"] = (
            "Speed of fun and relaxing, such as Listen to Music, Dance, Massage, Look in Mirror and Admire.",
            "娱乐放松的速度，例如听音乐、跳舞、按摩、照镜子、鉴赏。"),
        ["Exercise"] = (
            "Speed of exercise and some chores, such as Work Out, Running, Do Push-ups, Manual Power Generation, Do Laundry, Clean and Take a Bath.",
            "运动和部分家务的速度，例如锻炼、跑步、做俯卧撑、手动发电、洗衣服、打扫卫生、洗个澡。"),
        ["Social"] = (
            "Speed of quiet time, such as Look Outside, Listen to Radio, Meditate, Journal and Do Yoga.",
            "安静独处的速度，例如观察窗外、听收音机、冥想、写日记、瑜伽。"),
        ["Treatment"] = (
            "Speed of taking medicine: Eat Vitamins and Take Meds.",
            "吃药的速度：吃维生素、吃治疗药。"),
        ["Move"] = (
            "Moving around the map and between floors. These last as long as the walk, so this setting changes nothing; use [Walking] instead.",
            "在地图上和楼层之间移动。这些动作的时长由走路决定，所以此项不起作用，请改用 [Walking]。"),
        ["Other"] = (
            "Speed of everything else, such as Search, Harvest, Pry Open, Wash Hands, Use Toilet, Unlock Furniture, Planting and short moments like Hungry... and Tired...",
            "其他动作的速度，例如搜索、收获、强行撬开、洗手、坐下就开始……（上厕所）、解锁家具、正在种植，以及“好饿！”“好困！”这类短暂反应。"),
        ["Free"] = (
            "No action in the game uses this type, so this setting changes nothing.",
            "目前游戏中没有动作属于此类型，所以此项不起作用。"),
        ["RepairFurniture"] = (
            "Speed of repairs and building work, such as Repair, Fix Door, Restore Power, Reinforce Door, Install Furniture, Move Furniture, Dismantle and Scrap.",
            "修理和建造的速度，例如修理设施、修补房门、修复电力、加固房门、安装家具、移动家具、拆除设施、回收。"),
    };

    public override void Load()
    {
        var log = new SpeedLog(Log, Path.Combine(Paths.BepInExRootPath, "action-speed.log"));

        log.TrimIfLarge();

        var settings = BindSettings(log.Write);
        var isReopenFree = Config.Bind("Actions", "FreeSecondSearch", true,
            "When true, searching a container you have already searched opens it at once: no time passes, so an expedition adds no exposure for it. When false, every search takes the game's time.\n" +
            "设为 true 时，搜寻已经搜过的容器会立即打开：不花时间，外出探索时也不会因此增加暴露值。设为 false 时，每次搜寻都按游戏原本的时间进行。").Value;
        var walking = BindWalking();
        var crafting = BindKind("Crafting", "crafting at a workbench", "工作台手工制作", log.Write);
        var brewing = BindKind("Brewing", "brewing alcohol", "酿酒", log.Write);
        var cooking = BindKind("Cooking", "cooking at a stove or hot pot", "在燃气灶或火锅上烹饪", log.Write);
        var harmony = new Harmony(Guid);

        log.Write($"===== Action Speed {Version} started: {Describe(settings)}, free second search {(isReopenFree ? "on" : "off")}, walking x{walking.Player:0.##} (robot x{walking.Robot:0.##}, survivors x{walking.Survivor:0.##}), crafting x{crafting.Speed:0.##}, brewing x{brewing.Speed:0.##}, cooking x{cooking.Speed:0.##}");
        EffectClockPatch.Apply(harmony, settings, isReopenFree, log.Write, log.WriteFileOnly);
        WalkPatch.Apply(harmony, walking, log.Write, log.WriteFileOnly);
        CraftPatch.Apply(harmony, crafting, log.Write, log.WriteFileOnly);
        BrewPatch.Apply(harmony, brewing, log.Write, log.WriteFileOnly);
        CookPatch.Apply(harmony, cooking, log.Write, log.WriteFileOnly);

        SpeedTicker.Log = log.Write;
        SpeedTicker.Writer = new ActionWriter(settings, Path.Combine(Paths.BepInExRootPath, "action-speed-actions.txt"), log.Write, EffectClockPatch.Publish,
            isReopenFree ? EffectClockPatch.PublishSearches : _ => { });
        AddComponent<SpeedTicker>();
    }

    private SpeedSettings BindSettings(Action<string> log)
    {
        var speedByType = new Dictionary<int, float>();

        foreach (var (type, name) in ActionTypes())
        {
            var fallback = DefaultSpeeds.TryGetValue(name, out var preset) ? preset : 1f;
            (string English, string Chinese) covers = TypeCovers.TryGetValue(name, out var text) ? text : (English:$"Speed of {name} actions.", Chinese: $"{name} 类动作的速度。");

            speedByType[type] = ActionSpeeds.Clamp(Config.Bind("Actions", name, fallback,
                $"{covers.English} {ActionRange}\n" +
                $"{covers.Chinese}{ActionRangeChinese}").Value);
        }

        var isFullBenefits = Config.Bind("Actions", "FullBenefits", true,
            "When true, a sped-up action still gives its full effect: a faster sleep rests you as much as a normal one. When false, a sped-up action ends early and gives only what it gave in that shorter time.\n" +
            "设为 true 时，加速的动作仍有完整效果：睡得更快也能休息得和平常一样足。设为 false 时，加速的动作会提前结束，只获得这段较短时间内的效果。").Value;
        var overrides = ActionSpeeds.ParseOverrides(Config.Bind("Actions", "Overrides", "",
            "Speeds for single actions, written as id:speed and separated by commas; these beat the type speeds above. For example, 112:4 makes Use Toilet four times as fast. Every action and its id is listed in BepInEx\\action-speed-actions.txt.\n" +
            "单独设置某个动作的速度，格式为 id:速度，多个用英文逗号分隔，优先于上面的类型速度。例如 112:4 让坐下就开始……（上厕所）变为四倍速。所有动作及其 id 列在 BepInEx\\action-speed-actions.txt 中。").Value);

        foreach (var rejected in overrides.Rejected) log($"ignored override '{rejected}', write it as id:speed with a speed above 0");

        return new SpeedSettings(speedByType, overrides.Speeds, isFullBenefits);
    }

    private WalkSpeeds BindWalking()
    {
        var player = Config.Bind("Walking", "Speed", 1.5f,
            $"How fast you walk, meaning the character you control. {WalkRange}\n" +
            $"你的行走速度，也就是你操控的那个角色。{WalkRangeChinese}").Value;
        var robot = Config.Bind("Walking", "RobotSpeed", 1.5f,
            $"How fast robots move. {WalkRange}\n" +
            $"机器人的移动速度。{WalkRangeChinese}").Value;
        var survivor = Config.Bind("Walking", "SurvivorSpeed", 1.5f,
            $"How fast other people at your base walk. Enemies always keep the game's speed. {WalkRange}\n" +
            $"基地里其他人的行走速度。敌人始终保持游戏原速。{WalkRangeChinese}").Value;

        return new WalkSpeeds(Walking.Clamp(player), Walking.Clamp(robot), Walking.Clamp(survivor));
    }

    /// <summary>Work the game times outside its action table; 1 leaves the game's timing exactly as it is.</summary>
    private KindSpeed BindKind(string key, string work, string workChinese, Action<string> log)
    {
        var speed = KindSpeed.From(Config.Bind("Speed", key, 1f,
            $"Speed of {work}. {ActionRange} 0 or less counts as 1.\n" +
            $"{workChinese}的速度。{ActionRangeChinese}0 或更小按 1 计算。").Value);
        var complaint = KindSpeeds.Complaint($"Speed/{key}", speed);

        if (complaint != null) log(complaint);

        return speed;
    }

    /// <summary>Type 0 is furniture use, which the game's enum leaves unnamed.</summary>
    private static IEnumerable<(int Type, string Name)> ActionTypes() =>
        new[] { (0, FurnitureUse) }.Concat(Enum.GetValues(typeof(ActionType)).Cast<ActionType>().Where(type => (int)type != 0).Select(type => ((int)type, type.ToString())));

    private static string Describe(SpeedSettings settings)
    {
        var faster = settings.SpeedByType.Where(pair => pair.Value != 1f).Select(pair => $"{ActionWriter.TypeName(pair.Key)} x{pair.Value:0.##}").ToList();
        var summary = faster.Count == 0 ? "every action type at the game's pace" : string.Join(", ", faster);

        return $"{summary}, {settings.Overrides.Count} overrides, full benefits {(settings.FullBenefits ? "on" : "off")}";
    }
}
