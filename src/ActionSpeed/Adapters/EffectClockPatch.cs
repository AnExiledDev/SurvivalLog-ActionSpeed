using System;
using System.Collections.Generic;
using ActionSpeed.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using Il2CppInterop.Runtime;

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
    private static readonly HookSwitch Switch = new();

    private static Harmony? _harmony;
    private static Action<string> _log = _ => { };
    private static HookFailures _failures = new(_ => { }, "");
    private static Action<string> _logFileOnly = _ => { };
    private static bool _hasLoggedFirstEffect;
    private static bool _hasLoggedFirstReopen;

    /// <summary>Where each field the hook reads sits in an effect's memory, found once when the hook goes in.</summary>
    private static int _argsTypeAt;
    private static int _fullLifeAt;
    private static int _lifeLeftAt;
    private static int _ownerIdAt;
    private static int _effectIdAt;

    /// <summary>Nothing is hooked until <see cref="Use"/> is given settings that need it.</summary>
    public static void Apply(Harmony harmony, Action<string> log, Action<string> logFileOnly)
    {
        _harmony = harmony;
        _log = log;
        _failures = new HookFailures(log, "running an action's effect faster");
        _logFileOnly = logFileOnly;
    }

    /// <summary>
    /// Called on the main thread at start and after each change. The hook goes in the first time some action runs its
    /// effect faster or slower, or a second search is free, which keeps every effect on the game's clock until then;
    /// once in, it returns at once while no setting needs it.
    /// </summary>
    public static void Use(SpeedSettings settings, bool isReopenFree, CharacterSpeeds workSpeeds)
    {
        var needsClock = ActionSpeeds.NeedsClock(settings) || !workSpeeds.IsGamePace;

        if (Switch.Use(needsClock || isReopenFree))
        {
            if (!Hook()) return;

            Switch.Placed();
        }

        if (!Switch.IsOn)
        {
            _log(settings.FullBenefits ? "every action effect runs on the game's clock" : "full benefits off, sped-up actions end early and give less");

            return;
        }

        _log(needsClock ? "sped-up actions give everything they give at the game's pace" : "action effects hooked for free second searches only");
    }

    private static bool Hook()
    {
        if (_harmony == null) return false;

        try
        {
            FindFields();

            var target = AccessTools.Method(typeof(BTEffectManager), nameof(BTEffectManager.UpdateNode), new[] { typeof(BtConfig), typeof(EffectBtArgs), typeof(float), typeof(float) })
                         ?? throw new MissingMethodException("BTEffectManager.UpdateNode(BtConfig, EffectBtArgs, float, float)");

            _harmony.Patch(target, prefix: new HarmonyMethod(typeof(EffectClockPatch), nameof(RunFaster)));
        }
        catch (Exception ex)
        {
            _log($"could not hook action effects, sped-up actions keep the game's duration: {ex.Message}");

            return false;
        }

        PatchProgressBar(_harmony, _log);

        return true;
    }

    /// <summary>Each field is looked up on the class that declares it; a field the game no longer has stops the hook going in.</summary>
    private static void FindFields()
    {
        _argsTypeAt = OffsetOf(Il2CppClassPointerStore<EffectBtArgs>.NativeClassPtr, "<ArgsType>k__BackingField");
        _fullLifeAt = OffsetOf(Il2CppClassPointerStore<GameCore.HotUpdate.BtArgs>.NativeClassPtr, "<MaxLifeDuration>k__BackingField");
        _lifeLeftAt = OffsetOf(Il2CppClassPointerStore<GameCore.HotUpdate.BtArgs>.NativeClassPtr, "<CurLifeDuration>k__BackingField");
        _ownerIdAt = OffsetOf(Il2CppClassPointerStore<GameCore.HotUpdate.NodeArgs>.NativeClassPtr, "<OwnerId>k__BackingField");
        _effectIdAt = OffsetOf(Il2CppClassPointerStore<GameCore.HotUpdate.BaseEntity>.NativeClassPtr, "<InstanceId>k__BackingField");
    }

    private static int OffsetOf(IntPtr declaringClass, string name)
    {
        var field = declaringClass == IntPtr.Zero ? IntPtr.Zero : IL2CPP.GetIl2CppField(declaringClass, name);
        var offset = field == IntPtr.Zero ? 0 : (int)IL2CPP.il2cpp_field_get_offset(field);

        return offset > 0 ? offset : throw new MissingFieldException("effect", name);
    }

    private static void PatchProgressBar(Harmony harmony, Action<string> log)
    {
        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(BaseAgent), nameof(BaseAgent.ShowUnitHeadBar)),
                prefix: new HarmonyMethod(typeof(EffectClockPatch), nameof(ShortenBar)));
            log("progress bars of sped-up actions fill with the action");
        }
        catch (Exception ex)
        {
            log($"could not hook the progress bar, sped-up actions finish before their bar fills: {ex.Message}");
        }
    }

    /// <summary>The bar is sized from the action's table length just as the action starts, so the owner's current action is the one it shows.</summary>
    private static void ShortenBar(BaseAgent __instance, ref float during)
    {
        if (!Switch.IsOn) return;

        try
        {
            if (__instance == null) return;

            var action = AgentTools.GetAgentComponent<ActionComponent>(__instance);

            during = EffectClock.BarSeconds(during, Clock.ClockOfAction(action?.ActionConfig?.ID, new BarOwner(action, __instance.InstanceId)));
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    /// <summary>The clock of every action that does not run on the game's; the action table is read after the hook goes in.</summary>
    public static void Publish(IReadOnlyDictionary<int, float> clockByAction) => Clock.Publish(clockByAction);

    public static void PublishSearches(IReadOnlySet<int> searchActions) => Clock.PublishSearches(searchActions);

    public static void PublishWorkSpeeds(CharacterSpeeds workSpeeds) => Clock.PublishWorkSpeeds(workSpeeds);

    /// <summary>The game marks a container searched when its bag first opens.</summary>
    private static bool IsTargetSearched(ActionComponent? action)
    {
        if (action == null) return false;

        var target = action.GetTarget();

        return target.Item2 == InstanceIdType.Furniture && target.Item1?.TryCast<Furniture>()?.HasSearched == true;
    }

    /// <summary>Called every few seconds so effects that ended are forgotten.</summary>
    public static void ForgetEnded() => Clock.ForgetEnded();

    /// <summary>
    /// Runs inside the game's own call, for every effect every frame; an escaping exception would reach native code.
    /// While no setting needs it, it returns before reading anything.
    /// </summary>
    private static unsafe void RunFaster(BTEffectManager __instance, EffectBtArgs args, ref float deltaTime)
    {
        if (!Switch.IsOn) return;

        try
        {
            if (args == null) return;

            var effect = new RunningEffect((byte*)args.Pointer, __instance);

            deltaTime = Clock.StepOf(effect, deltaTime, out var clock);

            if (clock == 1f) return;

            if (clock == EffectClock.Instant && !_hasLoggedFirstReopen)
            {
                _hasLoggedFirstReopen = true;
                _logFileOnly("first search of a container searched before, it ends at once");
            }

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
    /// Reads the game's fields straight from memory at the offsets found when the hook went in. Going through the
    /// runtime for each field costs a native call per read, which adds up when every effect asks every frame.
    /// </summary>
    private readonly unsafe struct RunningEffect : IRunningEffect
    {
        private readonly byte* _args;
        private readonly BTEffectManager _manager;

        public RunningEffect(byte* args, BTEffectManager manager)
        {
            _args = args;
            _manager = manager;
        }

        public bool IsActionEffect => *(int*)(_args + _argsTypeAt) == (int)EffectArgsType.Action;

        public float FullLife => *(float*)(_args + _fullLifeAt);

        public float LifeLeft => *(float*)(_args + _lifeLeftAt);

        public long EffectId => *(long*)(_args + _effectIdAt);

        /// <summary>The game builds a new effect manager with each loaded game, so its address tells one game from the next.</summary>
        public long WorldId => _manager == null ? 0L : _manager.Pointer.ToInt64();

        private long OwnerId => *(long*)(_args + _ownerIdAt);

        /// <summary>The game sets a character's current action just before starting its effect, so a fresh effect belongs to it.</summary>
        public int? OwnersActionId() => OwnersAction()?.ActionConfig?.ID;

        public bool IsOwnersTargetSearched() => IsTargetSearched(OwnersAction());

        public Character Owner() => Characters.Of(OwnerId);

        public int TimeScale() => Toolset._TimeScale;

        private ActionComponent? OwnersAction()
        {
            if (!BaseSingleton<BattleLogicWorld>.IsInstanceCreated) return null;

            var owner = BaseSingleton<BattleLogicWorld>.Instance._AgentManager?.GetAgent(OwnerId);

            return owner == null ? null : AgentTools.GetAgentComponent<ActionComponent>(owner);
        }
    }

    /// <summary>The character whose progress bar is being sized, and its current action.</summary>
    private readonly struct BarOwner : IEffectOwner
    {
        private readonly ActionComponent? _action;
        private readonly long _ownerId;

        public BarOwner(ActionComponent? action, long ownerId)
        {
            _action = action;
            _ownerId = ownerId;
        }

        public bool IsOwnersTargetSearched() => IsTargetSearched(_action);

        public Character Owner() => Characters.Of(_ownerId);
    }
}
