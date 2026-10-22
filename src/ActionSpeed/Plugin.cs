using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionSpeed.Adapters;
using ActionSpeed.Core;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ActionSpeed;

[BepInPlugin(Guid, "Action Speed", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.actionspeed";
    private const string Version = "0.8.0";
    private const string ActionRange = "From 0.25 to 20: 1 is the game's pace, 2 is twice as fast.";
    private const string ActionRangeChinese = "范围 0.25 到 20：1 为游戏原速，2 为两倍速。";
    private const string WalkRange = "From 0.25 to 5: 1 is the game's speed, 2 is twice as fast.";
    private const string WalkRangeChinese = "范围 0.25 到 5：1 为游戏原速，2 为两倍速。";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var log = new SpeedLog(Log, Path.Combine(Paths.BepInExRootPath, "action-speed.log"));

        log.TrimIfLarge();

        var actions = BindActions();
        var isReopenFree = Config.Bind("Actions", "FreeSecondSearch", false,
            "When true, searching a container you have already searched opens it at once: no time passes, so an expedition adds no exposure for it. When false, every search takes the game's time.\n" +
            "设为 true 时，搜寻已经搜过的容器会立即打开：不花时间，外出探索时也不会因此增加暴露值。设为 false 时，每次搜寻都按游戏原本的时间进行。");
        var walking = BindWalking();
        var crafting = BindKind("Crafting", "crafting at a workbench", "工作台手工制作");
        var harmony = new Harmony(Guid);
        var writer = new ActionWriter(Path.Combine(Paths.BepInExRootPath, "action-speed-actions.txt"), log.Write, EffectClockPatch.Publish, EffectClockPatch.PublishSearches);

        log.Write($"===== Action Speed {Version} started");
        actions.Speeds.MoveOldSettings(Config, log.Write);
        EffectClockPatch.Apply(harmony, log.Write, log.WriteFileOnly);
        WalkPatch.Apply(harmony, log.Write, log.WriteFileOnly);
        CraftPatch.Apply(harmony, log.Write, log.WriteFileOnly);

        OptionalLive.Watch(() =>
        {
            var settings = ReadActions(actions, log.Write);

            log.Write($"{Describe(settings)}, free second search {(isReopenFree.Value ? "on" : "off")}");
            writer.Use(settings, isReopenFree.Value);
            EffectClockPatch.Use(settings, isReopenFree.Value);
        }, actions.All.Append(isReopenFree).ToArray());

        OptionalLive.Watch(() => WalkPatch.Use(ReadWalking(walking)), walking.Player, walking.Robot, walking.Survivor);
        OptionalLive.Watch(crafting, speed => CraftPatch.Use(ReadKind("Crafting", speed, log.Write)));

        SpeedTicker.Log = log.Write;
        SpeedTicker.Writer = writer;
        AddComponent<SpeedTicker>();
    }

    private ActionEntries BindActions()
    {
        var speeds = ActionSettings.Bind(Config);
        var isFullBenefits = Config.Bind("Actions", "FullBenefits", true,
            "When true, a sped-up action still gives its full effect: a faster sleep rests you as much as a normal one. When false, a sped-up action ends early and gives only what it gave in that shorter time.\n" +
            "设为 true 时，加速的动作仍有完整效果：睡得更快也能休息得和平常一样足。设为 false 时，加速的动作会提前结束，只获得这段较短时间内的效果。");
        var overrides = Config.Bind("Actions", "Overrides", "",
            "Speeds for single actions, written as id:speed and separated by commas; these beat every speed setting. For example, 112:4 makes Use Toilet four times as fast. Every action and its id is listed in BepInEx\\action-speed-actions.txt.\n" +
            "单独设置某个动作的速度，格式为 id:速度，多个用英文逗号分隔，优先于所有速度设置。例如 112:4 让坐下就开始……（上厕所）变为四倍速。所有动作及其 id 列在 BepInEx\\action-speed-actions.txt 中。");

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
        var player = Config.Bind("Walking", "Speed", 1f,
            $"How fast you walk, meaning the character you control. {WalkRange}\n" +
            $"你的行走速度，也就是你操控的那个角色。{WalkRangeChinese}");
        var robot = Config.Bind("Walking", "RobotSpeed", 1f,
            $"How fast robots move. {WalkRange}\n" +
            $"机器人的移动速度。{WalkRangeChinese}");
        var survivor = Config.Bind("Walking", "SurvivorSpeed", 1f,
            $"How fast other people at your base walk. Enemies always keep the game's speed. {WalkRange}\n" +
            $"基地里其他人的行走速度。敌人始终保持游戏原速。{WalkRangeChinese}");

        return new WalkEntries(player, robot, survivor);
    }

    private static WalkSpeeds ReadWalking(WalkEntries walking) =>
        new(Walking.Clamp(walking.Player.Value), Walking.Clamp(walking.Robot.Value), Walking.Clamp(walking.Survivor.Value));

    /// <summary>Work the game times outside its action table; 1 leaves the game's timing exactly as it is.</summary>
    private ConfigEntry<float> BindKind(string key, string work, string workChinese) =>
        Config.Bind("Speed", key, 1f,
            $"Speed of {work}. {ActionRange} 0 or less counts as 1.\n" +
            $"{workChinese}的速度。{ActionRangeChinese}0 或更小按 1 计算。");

    private static KindSpeed ReadKind(string key, float value, Action<string> log)
    {
        var speed = KindSpeed.From(value);
        var complaint = KindSpeeds.Complaint($"Speed/{key}", speed);

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
}
