using System;
using System.Collections.Generic;
using ActionSpeed.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace ActionSpeed.Adapters;

/// <summary>
/// Runs a sped-up action's effect on a faster clock, so it gives everything it gives at the game's pace in less time.
/// Every action starts an effect that lasts the action's table duration, and the effect manager's per-frame update
/// hands one time step to all of it: the effect's remaining life, its delayed triggers and the timers that pay out
/// sleep, food and the rest. Scaling that one step moves them all together, so the action ends after the same
/// number of payouts it would have had. Static because Harmony patches are.
/// </summary>
internal static class EffectClockPatch
{
    /// <summary>The game's marker for an effect that lasts until something ends it.</summary>
    private const float Endless = -1f;

    private static readonly ClockMemo Clocks = new();

    private static IReadOnlyDictionary<int, float> _clockByAction = new Dictionary<int, float>();
    private static HookFailures _failures = new(_ => { }, "");
    private static Action<string> _logFileOnly = _ => { };
    private static bool _hasLoggedFirstEffect;

    /// <summary>Nothing is hooked unless some action runs its effect faster or slower, which keeps every effect on the game's clock.</summary>
    public static void Apply(Harmony harmony, SpeedSettings settings, Action<string> log, Action<string> logFileOnly)
    {
        if (!ActionSpeeds.NeedsClock(settings))
        {
            log(settings.FullBenefits ? "every action effect runs on the game's clock" : "full benefits off, sped-up actions end early and give less");

            return;
        }

        _failures = new HookFailures(log, "running an action's effect faster");
        _logFileOnly = logFileOnly;

        try
        {
            var target = AccessTools.Method(typeof(BTEffectManager), nameof(BTEffectManager.UpdateNode), new[] { typeof(BtConfig), typeof(EffectBtArgs), typeof(float), typeof(float) })
                         ?? throw new MissingMethodException("BTEffectManager.UpdateNode(BtConfig, EffectBtArgs, float, float)");

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(EffectClockPatch), nameof(RunFaster)));
            log("full benefits on, sped-up actions give everything they give at the game's pace");
        }
        catch (Exception ex)
        {
            log($"could not hook action effects, sped-up actions keep the game's duration: {ex.Message}");
        }
    }

    /// <summary>The clock of every action that does not run on the game's; the action table is read after the hook goes in.</summary>
    public static void Publish(IReadOnlyDictionary<int, float> clockByAction) => _clockByAction = clockByAction;

    /// <summary>Called every few seconds so effects that ended are forgotten.</summary>
    public static void ForgetEnded() => Clocks.Age();

    /// <summary>Runs inside the game's own call, for every effect every frame; an escaping exception would reach native code.</summary>
    private static void RunFaster(EffectBtArgs args, ref float deltaTime)
    {
        try
        {
            if (args == null || args.ArgsType != EffectArgsType.Action || args.MaxLifeDuration == Endless) return;

            var clock = ClockOf(args);

            if (clock == 1f) return;

            deltaTime *= clock;

            if (_hasLoggedFirstEffect) return;

            _hasLoggedFirstEffect = true;
            _logFileOnly($"first sped-up effect: action effect runs x{clock:0.##}, its full length is {args.MaxLifeDuration:0.##}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static float ClockOf(EffectBtArgs args)
    {
        var effectId = args.InstanceId;
        var isFresh = args.CurLifeDuration == args.MaxLifeDuration;

        if (Clocks.TryRecall(effectId, isFresh, out var clock)) return clock;

        clock = ClockOfOwnersAction(args.OwnerId);
        Clocks.Remember(effectId, clock);

        return clock;
    }

    /// <summary>The game sets a character's current action just before starting its effect, so a fresh effect belongs to it.</summary>
    private static float ClockOfOwnersAction(long ownerId)
    {
        if (!BaseSingleton<BattleLogicWorld>.IsInstanceCreated) return 1f;

        var owner = BaseSingleton<BattleLogicWorld>.Instance._AgentManager?.GetAgent(ownerId);
        var action = owner == null ? null : AgentTools.GetAgentComponent<ActionComponent>(owner)?.ActionConfig;

        return action != null && _clockByAction.TryGetValue(action.ID, out var clock) ? clock : 1f;
    }
}
