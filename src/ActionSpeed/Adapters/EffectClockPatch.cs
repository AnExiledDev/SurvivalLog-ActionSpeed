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
    private static readonly EffectClock Clock = new();

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
    public static void Publish(IReadOnlyDictionary<int, float> clockByAction) => Clock.Publish(clockByAction);

    /// <summary>Called every few seconds so effects that ended are forgotten.</summary>
    public static void ForgetEnded() => Clock.ForgetEnded();

    /// <summary>Runs inside the game's own call, for every effect every frame; an escaping exception would reach native code.</summary>
    private static void RunFaster(EffectBtArgs args, ref float deltaTime)
    {
        try
        {
            if (args == null) return;

            var effect = new RunningEffect(args);
            var clock = Clock.ClockOf(effect);

            if (clock == 1f) return;

            deltaTime = EffectClock.Step(deltaTime, clock, EffectClock.LongestStep(Toolset._TimeScale), effect.LifeLeft);

            if (_hasLoggedFirstEffect) return;

            _hasLoggedFirstEffect = true;
            _logFileOnly($"first sped-up effect: action effect runs x{clock:0.##}, its full length is {effect.FullLife:0.##}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    /// <summary>
    /// Reads the game's fields straight from memory. Its property getters go through a runtime call that also
    /// allocates, which adds up when every effect asks every frame.
    /// </summary>
    private readonly struct RunningEffect : IRunningEffect
    {
        private readonly EffectBtArgs _args;

        public RunningEffect(EffectBtArgs args) => _args = args;

        public bool IsActionEffect => _args._ArgsType_k__BackingField == EffectArgsType.Action;

        public float FullLife => _args._MaxLifeDuration_k__BackingField;

        public float LifeLeft => _args._CurLifeDuration_k__BackingField;

        public long EffectId => _args._InstanceId_k__BackingField;

        /// <summary>The game sets a character's current action just before starting its effect, so a fresh effect belongs to it.</summary>
        public int? OwnersActionId()
        {
            if (!BaseSingleton<BattleLogicWorld>.IsInstanceCreated) return null;

            var owner = BaseSingleton<BattleLogicWorld>.Instance._AgentManager?.GetAgent(_args._OwnerId_k__BackingField);

            return owner == null ? null : AgentTools.GetAgentComponent<ActionComponent>(owner)?.ActionConfig?.ID;
        }
    }
}
