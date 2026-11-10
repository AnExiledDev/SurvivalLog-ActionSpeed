using System;
using ActionSpeed.Core;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;

namespace ActionSpeed.Adapters;

/// <summary>
/// Shortens crafting at a workbench. When a craft starts, the game sets the bench's clock to 0 and its length to the
/// global ProductionTime (game seconds, less any talent), and the bench's update finishes the craft once the clock
/// reaches that length. This shortens the length right after a craft starts, once per craft, and tells the bench window
/// the shortened length so its progress bar fills as the craft finishes (<see cref="CraftBar"/>). Static because
/// Harmony patches are.
/// </summary>
internal static class CraftPatch
{
    private static readonly HookSwitch Switch = new();

    private static KindSpeed _speed = KindSpeed.GamePace;
    private static PatchKit? _hooks;
    private static Action<string> _log = _ => { };
    private static Action<string> _logFileOnly = _ => { };
    private static CraftBar _bar = new(KindSpeed.GamePace);
    private static bool _hasLoggedFirstCraft;
    private static bool _hasLoggedFirstBar;

    /// <summary>Nothing is hooked until <see cref="Use"/> is given a speed that is not the game's pace.</summary>
    public static void Apply(PatchKit hooks, Action<string> log, Action<string> logFileOnly)
    {
        _hooks = hooks;
        _log = log;
        _logFileOnly = logFileOnly;
    }

    /// <summary>
    /// Called on the main thread at start and after each change; crafts started after it go at the new speed. The hooks
    /// go in the first time the speed is not the game's pace, which is what keeps the default timing exactly the game's;
    /// once in, at the game's pace they return at once and leave the craft and its bar as the game set them.
    /// </summary>
    public static void Use(KindSpeed speed)
    {
        _speed = speed;
        _bar = new CraftBar(speed);

        if (Switch.Use(!speed.IsGamePace))
        {
            if (!Hook()) return;

            Switch.Placed();
        }

        if (!Switch.IsOn)
        {
            _log("crafting left at the game's pace");

            return;
        }

        _log($"crafting x{speed.Speed:0.##}");
    }

    private static bool Hook()
    {
        if (_hooks == null) return false;

        var isHooked = _hooks.Patch(typeof(CraftPatch), typeof(ToolTableManager), nameof(ToolTableManager.StartProduction),
            prefix: nameof(Remember), postfix: nameof(Shorten), finalizer: nameof(EndStart), consequence: "crafting stays at the game's pace");

        if (!isHooked) return false;

        // The reducer itself is compiled into this trigger, so a hook on the reducer never runs.
        _hooks.Patch(typeof(CraftPatch), typeof(Reducer_Web_ToolTable), nameof(Reducer_Web_ToolTable.Trigger_RA_CheckResult),
            prefix: nameof(ShowShortened), consequence: "the workbench progress bar fills at the game's pace");

        return true;
    }

    /// <summary>The bench before a start request; null when it could not be read, which leaves the craft alone.</summary>
    internal sealed record Before(CraftSnapshot? Bench);

    /// <summary>Runs inside the game's call; an escaping exception would reach native code.</summary>
    private static void Remember(ToolTableManager __instance, long furnitureId, out Before? __state)
    {
        __state = null;

        if (!Switch.IsOn) return;

        try
        {
            _bar.Starting(furnitureId);
            __state = new Before(Snapshot(Bench(__instance, furnitureId)));
        }
        catch (Exception ex)
        {
            _hooks?.Fail("reading a workbench before a craft", ex);
        }
    }

    private static void Shorten(ToolTableManager __instance, long furnitureId, Before? __state)
    {
        if (!Switch.IsOn) return;

        var gameDuration = 0f;
        float? shortened = null;

        try
        {
            var bench = Bench(__instance, furnitureId);

            if (__state == null || bench == null || !KindSpeeds.IsFreshCraft(__state.Bench, new CraftSnapshot(bench.State, bench.MakingTime))) return;

            gameDuration = bench.MakingDuration;
            bench.MakingDuration = KindSpeeds.ScaleSeconds(gameDuration, _speed);
            shortened = bench.MakingDuration;

            if (_hasLoggedFirstCraft) return;

            _hasLoggedFirstCraft = true;
            _logFileOnly($"first craft: game length {gameDuration:0.##}s, now {bench.MakingDuration:0.##}s");
        }
        catch (Exception ex)
        {
            _hooks?.Fail("shortening a craft", ex);
        }
        finally
        {
            _bar.Started(furnitureId, gameDuration, shortened);
        }
    }

    /// <summary>Runs after the start request even when it threw, which skips <see cref="Shorten"/>.</summary>
    private static void EndStart()
    {
        if (Switch.IsOn) _bar.StartEnded();
    }

    /// <summary>Runs inside the game's call; an escaping exception would reach native code.</summary>
    private static void ShowShortened(BaseReduxAction __0)
    {
        if (!Switch.IsOn) return;

        try
        {
            var result = __0?.TryCast<Ac_ToolTable_CheckResult>();

            if (result == null || !result.IsCheck) return;

            var told = result.MakeTime;

            result.MakeTime = _bar.Shown(result.FurnitureId, told);

            if (_hasLoggedFirstBar) return;

            _hasLoggedFirstBar = true;
            _logFileOnly($"first workbench bar: told {told:0.##}s, shown {result.MakeTime:0.##}s");
        }
        catch (Exception ex)
        {
            _hooks?.Fail("showing a shortened craft on the workbench bar", ex);
        }
    }

    private static ToolTableData? Bench(ToolTableManager manager, long furnitureId)
    {
        var benches = manager.ToolTableMap;

        return benches != null && benches.TryGetValue(furnitureId, out var bench) ? bench : null;
    }

    private static CraftSnapshot? Snapshot(ToolTableData? bench) => bench == null ? null : new CraftSnapshot(bench.State, bench.MakingTime);
}
