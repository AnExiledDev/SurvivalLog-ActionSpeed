using System;
using ActionSpeed.Core;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace ActionSpeed.Adapters;

/// <summary>
/// Shortens cooking at a stove or hot pot. The game hands a cook its length in game seconds when it starts, and the
/// cook's progress is the game clock since the start divided by that length; fuel burns by progress, so a shorter
/// cook burns the same fuel per dish. Static because Harmony patches are.
/// </summary>
internal static class CookPatch
{
    private static KindSpeed _speed = KindSpeed.GamePace;
    private static HookFailures _failures = new(_ => { }, "");
    private static Action<string> _logFileOnly = _ => { };
    private static bool _hasLoggedFirstCook;

    /// <summary>At the game's pace nothing is hooked, which is what keeps the default timing exactly the game's.</summary>
    public static void Apply(Harmony harmony, KindSpeed speed, Action<string> log, Action<string> logFileOnly)
    {
        if (speed.IsGamePace)
        {
            log("cooking left at the game's pace");

            return;
        }

        _speed = speed;
        _failures = new HookFailures(log, "shortening a cook");
        _logFileOnly = logFileOnly;

        try
        {
            var target = AccessTools.Method(typeof(CookingComponent), nameof(CookingComponent.StartCooking))
                         ?? throw new MissingMethodException("CookingComponent.StartCooking");

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(CookPatch), nameof(Shorten)));
            log($"cooking x{speed.Speed:0.##}");
        }
        catch (Exception ex)
        {
            log($"could not hook cooking, it stays at the game's pace: {ex.Message}");
        }
    }

    /// <summary>Runs inside the game's own call; an escaping exception would reach native code.</summary>
    private static void Shorten(ref float cookDuration)
    {
        try
        {
            var gameDuration = cookDuration;

            cookDuration = KindSpeeds.ScaleSeconds(gameDuration, _speed);

            if (_hasLoggedFirstCook) return;

            _hasLoggedFirstCook = true;
            _logFileOnly($"first cook: game {gameDuration:0.##}s, now {cookDuration:0.##}s");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
