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
    private const string Version = "0.2.0";
    private const string FurnitureUse = "FurnitureUse";

    /// <summary>Short chores players ask to speed up; sleep, fun, workouts, talking and repairs keep the game's pace until set.</summary>
    private static readonly Dictionary<string, float> DefaultSpeeds = new() { [FurnitureUse] = 2f, ["Food"] = 2f, ["Treatment"] = 2f, ["Other"] = 2f };

    public override void Load()
    {
        var log = new SpeedLog(Log, Path.Combine(Paths.BepInExRootPath, "action-speed.log"));

        log.TrimIfLarge();

        var settings = BindSettings(log.Write);
        var walkSpeed = Walking.Clamp(Config.Bind("Walking", "Speed", 1.5f,
            $"How fast your character walks, from {Walking.Slowest} to {Walking.Fastest}. 1 is the game's speed, 2 is twice as fast. Survivors and the robot walk at the game's speed.").Value);

        var crafting = BindKind("Crafting", "crafting at a workbench", log.Write);
        var brewing = BindKind("Brewing", "brewing alcohol", log.Write);
        var cooking = BindKind("Cooking", "cooking at a stove or hot pot", log.Write);
        var harmony = new Harmony(Guid);

        log.Write($"===== Action Speed {Version} started: {Describe(settings)}, walking x{walkSpeed:0.##}, crafting x{crafting.Speed:0.##}, brewing x{brewing.Speed:0.##}, cooking x{cooking.Speed:0.##}");
        WalkPatch.Apply(harmony, walkSpeed, log.Write, log.WriteFileOnly);
        CraftPatch.Apply(harmony, crafting, log.Write, log.WriteFileOnly);
        BrewPatch.Apply(harmony, brewing, log.Write, log.WriteFileOnly);
        CookPatch.Apply(harmony, cooking, log.Write, log.WriteFileOnly);

        SpeedTicker.Log = log.Write;
        SpeedTicker.Writer = new ActionWriter(settings, Path.Combine(Paths.BepInExRootPath, "action-speed-actions.txt"), log.Write);
        AddComponent<SpeedTicker>();
    }

    private SpeedSettings BindSettings(Action<string> log)
    {
        var speedByType = new Dictionary<int, float>();

        foreach (var (type, name) in ActionTypes())
        {
            var fallback = DefaultSpeeds.TryGetValue(name, out var preset) ? preset : 1f;

            speedByType[type] = ActionSpeeds.Clamp(Config.Bind("Actions", name, fallback,
                $"Speed of {name} actions, from {ActionSpeeds.Slowest} to {ActionSpeeds.Fastest}. 1 is the game's pace, 2 takes half the in-game time. BepInEx/action-speed-actions.txt lists every action with its type.").Value);
        }

        var keepPaying = Config.Bind("Actions", "KeepTimedEffects", true,
            "Leave an action at the game's pace when its effect pays out over time (sleeping, resting, some meals), so speeding it up never shortens what it gives you.").Value;
        var overrides = ActionSpeeds.ParseOverrides(Config.Bind("Actions", "Overrides", "",
            "Speeds for single actions by id; these beat the type speed and KeepTimedEffects. Example: 112:4, 39:2. Ids are in BepInEx/action-speed-actions.txt.").Value);

        foreach (var rejected in overrides.Rejected) log($"ignored override '{rejected}', write it as id:speed with a speed above 0");

        return new SpeedSettings(speedByType, overrides.Speeds, keepPaying);
    }

    /// <summary>Work the game times outside its action table; 1 leaves the game's timing exactly as it is.</summary>
    private KindSpeed BindKind(string key, string work, Action<string> log)
    {
        var speed = KindSpeed.From(Config.Bind("Speed", key, 1f,
            $"Speed of {work}, from {ActionSpeeds.Slowest} to {ActionSpeeds.Fastest}. 1 is the game's pace, 2 takes half the in-game time. 0 or less counts as 1.").Value);
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

        return $"{summary}, {settings.Overrides.Count} overrides, timed effects {(settings.KeepPayingActions ? "kept at the game's pace" : "sped up too")}";
    }
}
