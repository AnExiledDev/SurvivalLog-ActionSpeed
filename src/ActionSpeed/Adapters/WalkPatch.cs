using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace ActionSpeed.Adapters;

/// <summary>
/// Scales the player character's walking speed where the game reads it. Each time a walk starts, the game asks the
/// character's attributes for move speed with its percentage bonus (this two-name lookup serves move speed alone)
/// and hands the answer to both the navigation agent and the walk animation, so the legs keep up. Other characters,
/// the robot included, walk at the game's speed. Static because Harmony patches are.
/// </summary>
internal static class WalkPatch
{
    private const int MaxLoggedFailures = 3;

    private static float _multiplier = 1f;
    private static Action<string> _log = _ => { };
    private static Action<string> _logFileOnly = _ => { };
    private static bool _hasLoggedFirstWalk;
    private static int _failures;

    public static void Apply(Harmony harmony, float multiplier, Action<string> log, Action<string> logFileOnly)
    {
        _multiplier = multiplier;
        _log = log;
        _logFileOnly = logFileOnly;

        if (multiplier == 1f)
        {
            log("walking speed left at the game's");

            return;
        }

        try
        {
            var target = AccessTools.Method(typeof(AttributeComponent), nameof(AttributeComponent.GetTotalValue_Float), new[] { typeof(AttrName), typeof(AttrName) })
                         ?? throw new MissingMethodException("AttributeComponent.GetTotalValue_Float(AttrName, AttrName)");

            harmony.Patch(target, postfix: new HarmonyMethod(typeof(WalkPatch), nameof(ScaleWalk)));
            log($"walking speed x{multiplier:0.##} for your character");
        }
        catch (Exception ex)
        {
            log($"could not hook walking speed, it stays at the game's: {ex.Message}");
        }
    }

    /// <summary>Runs inside the game's own call; an escaping exception would reach native code.</summary>
    private static void ScaleWalk(AttributeComponent __instance, AttrName attrName, AttrName ratioName, ref float __result)
    {
        if (attrName != AttrName.MoveSpeed || ratioName != AttrName.MoveSpeed_Ratio) return;

        try
        {
            if (__instance.ownerInstanceId != LeadingRoleId()) return;

            var gameSpeed = __result;

            __result = gameSpeed * _multiplier;

            if (_hasLoggedFirstWalk) return;

            _hasLoggedFirstWalk = true;
            _logFileOnly($"first walk: game speed {gameSpeed:0.##}, now {__result:0.##}");
        }
        catch (Exception ex)
        {
            _failures++;

            if (_failures <= MaxLoggedFailures) _log($"scaling a walk failed, it goes at the game's speed (logged {_failures} of {MaxLoggedFailures}): {ex}");
        }
    }

    /// <summary>-1 when no world is loaded, which matches no character.</summary>
    private static long LeadingRoleId()
    {
        if (!BaseSingleton<BattleLogicWorld>.IsInstanceCreated) return -1;

        return BaseSingleton<BattleLogicWorld>.Instance._AgentManager?.LeadingRoleId ?? -1;
    }
}
