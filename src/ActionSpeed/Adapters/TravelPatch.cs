using System;
using ActionSpeed.Core;
using GameCore.HotUpdate;
using HarmonyLib;

namespace ActionSpeed.Adapters;

/// <summary>
/// Scales the time a trip to or from an expedition place takes. The game asks one method for a place's trip time,
/// both when the trip is made (going out and coming home) and when the map shows it, so the time shown is the time
/// taken; a vehicle's discount is applied by the game afterwards, on top. Static because Harmony patches are.
/// </summary>
internal static class TravelPatch
{
    private static KindSpeed _speed = KindSpeed.GamePace;
    private static Harmony? _harmony;
    private static bool _isHooked;
    private static Action<string> _log = _ => { };
    private static Action<string> _logFileOnly = _ => { };
    private static HookFailures _failures = new(_ => { }, "");
    private static bool _hasLoggedFirstTrip;

    /// <summary>Nothing is hooked until <see cref="Use"/> is given a speed that is not the game's.</summary>
    public static void Apply(Harmony harmony, Action<string> log, Action<string> logFileOnly)
    {
        _harmony = harmony;
        _log = log;
        _logFileOnly = logFileOnly;
        _failures = new HookFailures(log, "changing an expedition trip's time");
    }

    /// <summary>Called on the main thread at start and after each change; trips started after it take the new time.</summary>
    public static void Use(KindSpeed speed)
    {
        _speed = speed;

        if (speed.IsGamePace)
        {
            _log("expedition travel takes the game's time");

            return;
        }

        if (!_isHooked && !Hook()) return;

        _log($"expedition travel x{speed.Speed:0.##}");
    }

    private static bool Hook()
    {
        if (_harmony == null) return false;

        try
        {
            var target = AccessTools.Method(typeof(MapPointRules), nameof(MapPointRules.GetTravelTime), new[] { typeof(Config_MapPoint) })
                         ?? throw new MissingMethodException("MapPointRules.GetTravelTime(Config_MapPoint)");

            _harmony.Patch(target, postfix: new HarmonyMethod(typeof(TravelPatch), nameof(ScaleTrip)));
        }
        catch (Exception ex)
        {
            _log($"could not hook expedition travel, it takes the game's time: {ex.Message}");

            return false;
        }

        _isHooked = true;

        return true;
    }

    /// <summary>Runs inside the game's own call; an escaping exception would reach native code.</summary>
    private static void ScaleTrip(ref int __result)
    {
        try
        {
            var gameSeconds = __result;

            __result = Travel.Seconds(gameSeconds, _speed);

            if (_hasLoggedFirstTrip || __result == gameSeconds) return;

            _hasLoggedFirstTrip = true;
            _logFileOnly($"first trip time asked for: game {gameSeconds} s, now {__result} s");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
