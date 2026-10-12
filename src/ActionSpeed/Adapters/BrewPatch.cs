using System;
using ActionSpeed.Core;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace ActionSpeed.Adapters;

/// <summary>
/// Shortens brewing. The game works out a brew's length in whole game hours in one place, the brew preview, and both
/// the brewing panel and the start of a brew read it from there, so shortening it there keeps the hours the panel
/// shows equal to the hours the brew takes. If that hook cannot be installed, the start of the brew is hooked instead
/// and only the panel's estimate stays at the game's. Static because Harmony patches are.
/// </summary>
internal static class BrewPatch
{
    private static KindSpeed _speed = KindSpeed.GamePace;
    private static HookFailures _failures = new(_ => { }, "");
    private static Action<string> _logFileOnly = _ => { };
    private static bool _hasLoggedFirstBrew;

    /// <summary>At the game's pace nothing is hooked, which is what keeps the default timing exactly the game's.</summary>
    public static void Apply(Harmony harmony, KindSpeed speed, Action<string> log, Action<string> logFileOnly)
    {
        if (speed.IsGamePace)
        {
            log("brewing left at the game's pace");

            return;
        }

        _speed = speed;
        _failures = new HookFailures(log, "shortening a brew");
        _logFileOnly = logFileOnly;

        try
        {
            harmony.Patch(Method(typeof(Furniture), nameof(Furniture.CalcBrewPreview)), postfix: new HarmonyMethod(typeof(BrewPatch), nameof(Shorten)));
            log($"brewing x{speed.Speed:0.##}");
        }
        catch (Exception previewFailure)
        {
            HookBrewStart(harmony, speed, log, previewFailure);
        }
    }

    private static void HookBrewStart(Harmony harmony, KindSpeed speed, Action<string> log, Exception previewFailure)
    {
        try
        {
            harmony.Patch(Method(typeof(BrewComponent), nameof(BrewComponent.BeginBrew)), prefix: new HarmonyMethod(typeof(BrewPatch), nameof(Shorten)));
            log($"brewing x{speed.Speed:0.##}, the brewing panel still shows the game's hours ({previewFailure.Message})");
        }
        catch (Exception ex)
        {
            log($"could not hook brewing, it stays at the game's pace: {previewFailure.Message}; {ex.Message}");
        }
    }

    private static System.Reflection.MethodInfo Method(Type type, string name) =>
        AccessTools.Method(type, name) ?? throw new MissingMethodException($"{type.Name}.{name}");

    /// <summary>
    /// A postfix on the preview (where totalHours is an out value) or a prefix on the start (where it is an
    /// argument); both read and write the same hours. Runs inside the game's own call, so nothing may escape. The first
    /// one is logged, so a player's log shows the hook ran and what it wrote back.
    /// </summary>
    private static void Shorten(ref int totalHours)
    {
        try
        {
            var gameHours = totalHours;

            totalHours = KindSpeeds.ScaleHours(gameHours, _speed);

            if (_hasLoggedFirstBrew) return;

            _hasLoggedFirstBrew = true;
            _logFileOnly($"first brew: game {gameHours}h, now {totalHours}h");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
