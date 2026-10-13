using System;
using System.Collections.Generic;
using ActionSpeed.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using BaseResident = GameCore.HotUpdate.Battle.Logic.DynamicNpc;

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

    private static readonly HashSet<Walker> FirstWalksLogged = new();

    private static WalkSpeeds _speeds = new(1f, 1f, 1f);
    private static Action<string> _log = _ => { };
    private static Action<string> _logFileOnly = _ => { };
    private static int _failures;

    public static void Apply(Harmony harmony, WalkSpeeds speeds, Action<string> log, Action<string> logFileOnly)
    {
        _speeds = speeds;
        _log = log;
        _logFileOnly = logFileOnly;

        if (speeds.IsGamePace)
        {
            log("walking speed left at the game's");

            return;
        }

        try
        {
            var target = AccessTools.Method(typeof(AttributeComponent), nameof(AttributeComponent.GetTotalValue_Float), new[] { typeof(AttrName), typeof(AttrName) })
                         ?? throw new MissingMethodException("AttributeComponent.GetTotalValue_Float(AttrName, AttrName)");

            harmony.Patch(target, postfix: new HarmonyMethod(typeof(WalkPatch), nameof(ScaleWalk)));
            log($"walking speed x{speeds.Player:0.##} for you, x{speeds.Robot:0.##} for the robot, x{speeds.Survivor:0.##} for survivors");
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
            var walker = WalkerOf(__instance.ownerInstanceId);
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

    /// <summary>With no world loaded nobody is known, and an unknown walker keeps the game's speed.</summary>
    private static Walker WalkerOf(long instanceId)
    {
        if (!BaseSingleton<BattleLogicWorld>.IsInstanceCreated) return Walker.Other;

        var agents = BaseSingleton<BattleLogicWorld>.Instance._AgentManager;

        if (agents == null) return Walker.Other;

        var isPlayer = instanceId == agents.LeadingRoleId;
        var resident = isPlayer ? null : agents.GetAgent(instanceId)?.TryCast<BaseResident>();

        return Walking.Classify(isPlayer, resident != null, resident?.IsRobot == true);
    }
}
