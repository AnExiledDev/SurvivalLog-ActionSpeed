using System;
using ActionSpeed.Core;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace ActionSpeed.Adapters;

/// <summary>
/// Shortens crafting at a workbench. When a craft starts, the game sets the bench's clock to 0 and its length to the
/// global ProductionTime (game seconds, less any talent), and the bench's update finishes the craft once the clock
/// reaches that length. This shortens the length right after a craft starts, once per craft. Static because Harmony
/// patches are.
/// </summary>
internal static class CraftPatch
{
    private static KindSpeed _speed = KindSpeed.GamePace;
    private static HookFailures _failures = new(_ => { }, "");
    private static Action<string> _logFileOnly = _ => { };
    private static bool _hasLoggedFirstCraft;

    /// <summary>At the game's pace nothing is hooked, which is what keeps the default timing exactly the game's.</summary>
    public static void Apply(Harmony harmony, KindSpeed speed, Action<string> log, Action<string> logFileOnly)
    {
        if (speed.IsGamePace)
        {
            log("crafting left at the game's pace");

            return;
        }

        _speed = speed;
        _failures = new HookFailures(log, "shortening a craft");
        _logFileOnly = logFileOnly;

        try
        {
            var target = AccessTools.Method(typeof(ToolTableManager), nameof(ToolTableManager.StartProduction))
                         ?? throw new MissingMethodException("ToolTableManager.StartProduction");

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(CraftPatch), nameof(Remember)), postfix: new HarmonyMethod(typeof(CraftPatch), nameof(Shorten)));
            log($"crafting x{speed.Speed:0.##}");
        }
        catch (Exception ex)
        {
            log($"could not hook crafting, it stays at the game's pace: {ex.Message}");
        }
    }

    /// <summary>The bench before a start request; null when it could not be read, which leaves the craft alone.</summary>
    internal sealed record Before(CraftSnapshot? Bench);

    /// <summary>Runs inside the game's own call; an escaping exception would reach native code.</summary>
    private static void Remember(ToolTableManager __instance, long furnitureId, out Before? __state)
    {
        __state = null;

        try
        {
            __state = new Before(Snapshot(Bench(__instance, furnitureId)));
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void Shorten(ToolTableManager __instance, long furnitureId, Before? __state)
    {
        if (__state == null) return;

        try
        {
            var bench = Bench(__instance, furnitureId);

            if (bench == null || !KindSpeeds.IsFreshCraft(__state.Bench, new CraftSnapshot(bench.State, bench.MakingTime))) return;

            var gameDuration = bench.MakingDuration;

            bench.MakingDuration = KindSpeeds.ScaleSeconds(gameDuration, _speed);

            if (_hasLoggedFirstCraft) return;

            _hasLoggedFirstCraft = true;
            _logFileOnly($"first craft: game length {gameDuration:0.##}s, now {bench.MakingDuration:0.##}s");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static ToolTableData? Bench(ToolTableManager manager, long furnitureId)
    {
        var benches = manager.ToolTableMap;

        return benches != null && benches.TryGetValue(furnitureId, out var bench) ? bench : null;
    }

    private static CraftSnapshot? Snapshot(ToolTableData? bench) => bench == null ? null : new CraftSnapshot(bench.State, bench.MakingTime);
}
