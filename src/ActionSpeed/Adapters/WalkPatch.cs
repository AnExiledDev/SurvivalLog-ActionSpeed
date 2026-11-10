using System;
using System.Collections.Generic;
using ActionSpeed.Core;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace ActionSpeed.Adapters;

/// <summary>
/// Scales walking speed where the game reads it, for the player, the robot and the base's survivors. When a walk
/// starts, the game asks the character's attributes for move speed with its percentage bonus (this two-name lookup
/// serves move speed alone) and hands the answer to both the navigation agent and the walk animation, so the legs
/// keep up; the robot's own movement reads the same value. Static because Harmony patches are.
/// </summary>
internal static class WalkPatch
{
    private static readonly HashSet<Character> FirstWalksLogged = new();

    private static CharacterSpeeds _speeds = new(1f, 1f, 1f);
    private static readonly HookSwitch Switch = new();

    private static PatchKit? _hooks;
    private static Action<string> _log = _ => { };
    private static Action<string> _logFileOnly = _ => { };

    /// <summary>Nothing is hooked until <see cref="Use"/> is given a speed that is not the game's.</summary>
    public static void Apply(PatchKit hooks, Action<string> log, Action<string> logFileOnly)
    {
        _hooks = hooks;
        _log = log;
        _logFileOnly = logFileOnly;
    }

    /// <summary>
    /// Called on the main thread at start and after each change; walks started after it go at the new speeds. The hook
    /// goes in the first time a speed is not the game's, and at the game's speed it returns at once on every walk.
    /// </summary>
    public static void Use(CharacterSpeeds speeds)
    {
        _speeds = speeds;

        if (Switch.Use(!speeds.IsGamePace))
        {
            if (!Hook()) return;

            Switch.Placed();
        }

        if (!Switch.IsOn)
        {
            _log("walking speed left at the game's");

            return;
        }

        _log($"walking speed x{speeds.Player:0.##} for you, x{speeds.Robot:0.##} for the robot, x{speeds.Survivor:0.##} for survivors");
    }

    private static bool Hook() =>
        _hooks != null &&
        _hooks.Patch(typeof(WalkPatch), typeof(AttributeComponent), nameof(AttributeComponent.GetTotalValue_Float), new[] { typeof(AttrName), typeof(AttrName) },
            postfix: nameof(ScaleWalk), consequence: "walking speed stays at the game's");

    /// <summary>
    /// Runs inside the game's call; an escaping exception would reach native code. While every speed is the
    /// game's, it returns before looking up who is walking.
    /// </summary>
    private static void ScaleWalk(AttributeComponent __instance, AttrName attrName, AttrName ratioName, ref float __result)
    {
        if (!Switch.IsOn || attrName != AttrName.MoveSpeed || ratioName != AttrName.MoveSpeed_Ratio) return;

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
            _hooks?.Fail("scaling a walk", ex);
        }
    }
}
