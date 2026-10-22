using System;
using System.Collections.Generic;
using ActionSpeed.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace ActionSpeed.Adapters;

/// <summary>
/// Scales walking speed where the game reads it, for the player, the robot and the base's survivors. When a walk
/// starts, the game asks the character's attributes for move speed with its percentage bonus (this two-name lookup
/// serves move speed alone) and hands the answer to both the navigation agent and the walk animation, so the legs
/// keep up; the robot's own movement reads the same value. Static because Harmony patches are.
/// </summary>
internal static class WalkPatch
{
    private const int MaxLoggedFailures = 3;

    private static readonly HashSet<Character> FirstWalksLogged = new();

    private static CharacterSpeeds _speeds = new(1f, 1f, 1f);
    private static Harmony? _harmony;
    private static bool _isHooked;
    private static Action<string> _log = _ => { };
    private static Action<string> _logFileOnly = _ => { };
    private static int _failures;

    /// <summary>Nothing is hooked until <see cref="Use"/> is given a speed that is not the game's.</summary>
    public static void Apply(Harmony harmony, Action<string> log, Action<string> logFileOnly)
    {
        _harmony = harmony;
        _log = log;
        _logFileOnly = logFileOnly;
    }

    /// <summary>
    /// Called on the main thread at start and after each change; walks started after it go at the new speeds. The hook
    /// goes in the first time a speed is not the game's, and at the game's speed it leaves every walk alone.
    /// </summary>
    public static void Use(CharacterSpeeds speeds)
    {
        _speeds = speeds;

        if (speeds.IsGamePace)
        {
            _log("walking speed left at the game's");

            return;
        }

        if (!_isHooked && !Hook()) return;

        _log($"walking speed x{speeds.Player:0.##} for you, x{speeds.Robot:0.##} for the robot, x{speeds.Survivor:0.##} for survivors");
    }

    private static bool Hook()
    {
        if (_harmony == null) return false;

        try
        {
            var target = AccessTools.Method(typeof(AttributeComponent), nameof(AttributeComponent.GetTotalValue_Float), new[] { typeof(AttrName), typeof(AttrName) })
                         ?? throw new MissingMethodException("AttributeComponent.GetTotalValue_Float(AttrName, AttrName)");

            _harmony.Patch(target, postfix: new HarmonyMethod(typeof(WalkPatch), nameof(ScaleWalk)));
        }
        catch (Exception ex)
        {
            _log($"could not hook walking speed, it stays at the game's: {ex.Message}");

            return false;
        }

        _isHooked = true;

        return true;
    }

    /// <summary>Runs inside the game's own call; an escaping exception would reach native code.</summary>
    private static void ScaleWalk(AttributeComponent __instance, AttrName attrName, AttrName ratioName, ref float __result)
    {
        if (attrName != AttrName.MoveSpeed || ratioName != AttrName.MoveSpeed_Ratio) return;

        try
        {
            var walker = Characters.Of(__instance.ownerInstanceId);
            var multiplier = Walking.SpeedFor(walker, _speeds);

            if (multiplier == 1f) return;

            var gameSpeed = __result;

            __result = gameSpeed * multiplier;

            if (FirstWalksLogged.Add(walker)) _logFileOnly($"first walk ({walker.ToString().ToLowerInvariant()}): game speed {gameSpeed:0.##}, now {__result:0.##}");
        }
        catch (Exception ex)
        {
            _failures++;

            if (_failures <= MaxLoggedFailures) _log($"scaling a walk failed, it goes at the game's speed (logged {_failures} of {MaxLoggedFailures}): {ex}");
        }
    }
}
