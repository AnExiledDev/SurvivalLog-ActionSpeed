using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionSpeed.Adapters;
using ActionSpeed.Core;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using HarmonyLib;
using static ActionSpeed.Adapters.WorldRule;

namespace ActionSpeed;

[BepInPlugin(Guid, "Action Speed", Version)]
[BepInDependency(ToolkitGuid, ">=0.19.0")]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.actionspeed";
    private const string Version = "0.10.2";
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";

    /// <summary>Lines add up over a long session, so past 1 MB the file is moved to action-speed.log.1 and started again.</summary>
    private const long MaxLogBytes = 1024 * 1024;

    /// <summary>The walking hook runs on every walk, so each kind of failure is written three times at most.</summary>
    private const int MaxLoggedFailures = 3;

    private const string ActionRange = "From 0.25 to 20: 1 is the game's pace, 2 is twice as fast.";
    private const string ActionRangeChinese = "范围 0.25 到 20：1 为游戏原速，2 为两倍速。";
    private const string WalkRange = "From 0.25 to 5: 1 is the game's speed, 2 is twice as fast.";
    private const string WalkRangeChinese = "范围 0.25 到 5：1 为游戏原速，2 为两倍速。";

    public override void Load()
    {
        var log = new ModLog(Log, "action-speed.log") { MaxFileBytes = MaxLogBytes };

        // Read before any Bind: binding a new key saves the file, which would hide that it came from an older version.
        var savedSettings = ActionSettings.ReadFile(Config.ConfigFilePath, log.Write);
        var actions = BindActions();
        var isReopenFree = Config.Bind("Actions", "FreeSecondSearch", false, Rule(
            "When true, searching a container you have already searched opens it at once. No game time passes, so it adds no exposure on an expedition. When false, every search takes its usual time, set by its speed setting or override.\n" +
            "设为 true 时，搜寻已经搜过的容器会立即打开：不花时间，外出探索时也不会因此增加暴露值。设为 false 时，每次搜寻都按平常的时间进行，也就是它的速度设置或单独设置给出的时间。"));
        var walking = BindWalking();
        var crafting = BindKind("Crafting", "Speed of crafting at a workbench.", "工作台手工制作的速度。");
        var travel = BindKind("Travel",
            "Speed of the trip between home and an expedition place, going out and coming home. The map shows the new time.",
            "往返家与外出地点的路程速度，去程和回程都算。地图上会显示新的时间。");
        var work = BindWork();
        var hooks = new PatchKit(new Harmony(Guid), log.Write, MaxLoggedFailures);
        var writer = new ActionWriter(Path.Combine(Paths.BepInExRootPath, "action-speed-actions.txt"), log.Write, EffectClockPatch.Publish, EffectClockPatch.PublishSearches);

        log.Write($"===== Action Speed {Version} started");
        actions.Speeds.MoveOldSettings(savedSettings, Config, log.Write);
        EffectClockPatch.Apply(hooks, log.Write, log.WriteFileOnly);
        WalkPatch.Apply(hooks, log.Write, log.WriteFileOnly);
        CraftPatch.Apply(hooks, log.Write, log.WriteFileOnly);
        TravelPatch.Apply(hooks, log.Write, log.WriteFileOnly);

        LiveSettings.Watch(() =>
        {
            var settings = ReadActions(actions, log.Write);
            var workSpeeds = ReadWork(work, log.Write);

            log.Write($"{Describe(settings)}, free second search {(isReopenFree.Value ? "on" : "off")}, work x{workSpeeds.Player:0.##} for you, x{workSpeeds.Robot:0.##} for the robot, x{workSpeeds.Survivor:0.##} for survivors");
            writer.Use(settings, isReopenFree.Value);
            EffectClockPatch.PublishWorkSpeeds(workSpeeds);
            EffectClockPatch.Use(settings, isReopenFree.Value, workSpeeds);
        }, actions.All.Append(isReopenFree).Concat(work.All).ToArray());

        LiveSettings.Watch(() => WalkPatch.Use(ReadWalking(walking)), walking.Player, walking.Robot, walking.Survivor);
        LiveSettings.Watch(crafting, speed => CraftPatch.Use(ReadKind("Speed/Crafting", speed, log.Write)));
        LiveSettings.Watch(travel, speed => TravelPatch.Use(ReadKind("Speed/Travel", speed, log.Write)));

        SpeedTicker.Log = log.Write;
        SpeedTicker.Writer = writer;
        AddComponent<SpeedTicker>();
    }

    private ActionEntries BindActions()
    {
        var speeds = ActionSettings.Bind(Config);
        var isFullBenefits = Config.Bind("Actions", "FullBenefits", true, Rule(
            "When true, a sped-up action still gives its full effect: a faster sleep rests you as much as a normal one. When false, a sped-up action ends early and gives only what it would in that shorter time.\n" +
            "设为 true 时，加速的动作仍有完整效果：睡得更快也能休息得和平常一样足。设为 false 时，加速的动作会提前结束，只获得这段较短时间内的效果。"));
        var overrides = Config.Bind("Actions", "Overrides", "",
            "Speeds for single actions, written as id:speed and separated by commas. An override beats every other speed setting. For example, 112:4 makes Use Toilet four times as fast. Every action and its id is listed in BepInEx\\action-speed-actions.txt.\n" +
            "单独设置某个动作的速度，格式为 id:速度，多个用英文逗号分隔。这里的设置优先于其他所有速度设置。例如 112:4 让上厕所变为四倍速。所有动作及其 id 列在 BepInEx\\action-speed-actions.txt 中。");

        return new ActionEntries(speeds, isFullBenefits, overrides);
    }

    private static SpeedSettings ReadActions(ActionEntries actions, Action<string> log)
    {
        var overrides = ActionSpeeds.ParseOverrides(actions.Overrides.Value);

        foreach (var rejected in overrides.Rejected) log($"ignored override '{rejected}', write it as id:speed with a speed above 0");

        return new SpeedSettings(actions.Speeds.Speeds(), overrides.Speeds, actions.FullBenefits.Value);
    }

    private WalkEntries BindWalking()
    {
        var player = Config.Bind("Walking", "Speed", 1f, Rule(
            $"How fast the character you control walks. {WalkRange}\n" +
            $"你操控的角色的行走速度。{WalkRangeChinese}"));
        var robot = Config.Bind("Walking", "RobotSpeed", 1f, Rule(
            $"How fast robots move. {WalkRange}\n" +
            $"机器人的移动速度。{WalkRangeChinese}"));
        var survivor = Config.Bind("Walking", "SurvivorSpeed", 1f, Rule(
            $"How fast other people at your base walk. Enemies always keep the game's speed. {WalkRange}\n" +
            $"基地里其他人的行走速度。敌人始终保持游戏原速。{WalkRangeChinese}"));

        return new WalkEntries(player, robot, survivor);
    }

    private WorkEntries BindWork()
    {
        var player = Config.Bind("Work", "Speed", 1f, Rule(
            $"How fast the character you control does every action, on top of each action's own speed. The action still gives its full effect. {ActionRange} 0 or less counts as 1.\n" +
            $"你操控的角色做每个动作的速度，叠加在各动作自己的速度之上。动作仍有完整效果。{ActionRangeChinese}0 或更小按 1 计算。"));
        var robot = Config.Bind("Work", "RobotSpeed", 1f, Rule(
            $"How fast robots do every action, on top of each action's own speed. The action still gives its full effect. {ActionRange} 0 or less counts as 1.\n" +
            $"机器人做每个动作的速度，叠加在各动作自己的速度之上。动作仍有完整效果。{ActionRangeChinese}0 或更小按 1 计算。"));
        var survivor = Config.Bind("Work", "SurvivorSpeed", 1f, Rule(
            $"How fast other people at your base do every action, on top of each action's own speed. The action still gives its full effect. {ActionRange} 0 or less counts as 1.\n" +
            $"基地里其他人做每个动作的速度，叠加在各动作自己的速度之上。动作仍有完整效果。{ActionRangeChinese}0 或更小按 1 计算。"));

        return new WorkEntries(player, robot, survivor);
    }

    private static CharacterSpeeds ReadWork(WorkEntries work, Action<string> log) =>
        new(ReadKind("Work/Speed", work.Player.Value, log).Speed,
            ReadKind("Work/RobotSpeed", work.Robot.Value, log).Speed,
            ReadKind("Work/SurvivorSpeed", work.Survivor.Value, log).Speed);

    private static CharacterSpeeds ReadWalking(WalkEntries walking) =>
        new(Walking.Clamp(walking.Player.Value), Walking.Clamp(walking.Robot.Value), Walking.Clamp(walking.Survivor.Value));

    /// <summary>Work the game times outside its action table; 1 leaves the game's timing exactly as it is.</summary>
    private ConfigEntry<float> BindKind(string key, string description, string descriptionChinese) =>
        Config.Bind("Speed", key, 1f, Rule(
            $"{description} {ActionRange} 0 or less counts as 1.\n" +
            $"{descriptionChinese}{ActionRangeChinese}0 或更小按 1 计算。"));

    private static KindSpeed ReadKind(string key, float value, Action<string> log)
    {
        var speed = KindSpeed.From(value);
        var complaint = KindSpeeds.Complaint(key, speed);

        if (complaint != null) log(complaint);

        return speed;
    }

    /// <summary>The common speed, then each group that differs from it, so the log line stays short.</summary>
    private static string Describe(SpeedSettings settings)
    {
        var common = settings.SpeedByGroup.Values.GroupBy(speed => speed).OrderByDescending(group => group.Count()).First().Key;
        var differ = settings.SpeedByGroup.Where(pair => pair.Value != common).Select(pair => $"{ActionGroups.Label(pair.Key)} x{pair.Value:0.##}").ToList();
        var others = differ.Count == 0 ? "" : $", except {string.Join(", ", differ)}";

        return $"most actions x{common:0.##}{others}, {settings.Overrides.Count} overrides, full benefits {(settings.FullBenefits ? "on" : "off")}";
    }

    private sealed record ActionEntries(ActionSettings Speeds, ConfigEntry<bool> FullBenefits, ConfigEntry<string> Overrides)
    {
        public IEnumerable<ConfigEntryBase> All => Speeds.All.Append(FullBenefits).Append(Overrides);
    }

    private sealed record WalkEntries(ConfigEntry<float> Player, ConfigEntry<float> Robot, ConfigEntry<float> Survivor);

    private sealed record WorkEntries(ConfigEntry<float> Player, ConfigEntry<float> Robot, ConfigEntry<float> Survivor)
    {
        public IEnumerable<ConfigEntryBase> All => new ConfigEntryBase[] { Player, Robot, Survivor };
    }
}
