using System;
using System.Collections.Generic;

namespace ActionSpeed.Core;

/// <summary>One effect as the game's per-frame effect update hands it over. Each member reads the game's memory, so
/// <see cref="EffectClock"/> asks for as few as it can before it decides.</summary>
public interface IRunningEffect
{
    /// <summary>Started by an action, as opposed to a buff, a piece of furniture or the world.</summary>
    bool IsActionEffect { get; }

    /// <summary>The effect's whole life: -1 when it lasts until something ends it, 0 when it never counts down.</summary>
    float FullLife { get; }

    float LifeLeft { get; }

    /// <summary>The game gives this id to a later effect once this one ends.</summary>
    long EffectId { get; }

    /// <summary>The action the effect's owner is doing now, or null when the owner or its action cannot be found.</summary>
    int? OwnersActionId();
}

/// <summary>
/// How fast each action effect runs, and how long a time step it may take in one frame. Every effect of every
/// character asks every frame, so the cheap answers come first and the owner's action is looked up only for an
/// effect that has just started or has not been seen yet.
/// </summary>
public sealed class EffectClock
{
    /// <summary>
    /// The shortest time between two payouts in any action's effect, in game seconds: exercise, reading and games
    /// pay every 360, and no item or furniture timer pays more often than every 600.
    /// </summary>
    public const float ShortestPayoutGameSeconds = 360f;

    private readonly ClockMemo _memo = new();

    private IReadOnlyDictionary<int, float> _clockByAction = new Dictionary<int, float>();

    /// <summary>The clock of every action that does not run on the game's.</summary>
    public void Publish(IReadOnlyDictionary<int, float> clockByAction) => _clockByAction = clockByAction;

    /// <summary>Called every few seconds so effects that ended are forgotten.</summary>
    public void ForgetEnded() => _memo.Age();

    /// <summary>
    /// Only an effect an action started, timed by the action's length, runs on the action's clock. One that lasts
    /// until something ends it, or never counts down, has no length to finish sooner. An effect that has just
    /// started may carry the id of one that ended, so it is looked up again.
    /// </summary>
    public float ClockOf<TEffect>(TEffect effect) where TEffect : IRunningEffect
    {
        if (_clockByAction.Count == 0 || !effect.IsActionEffect) return 1f;

        var fullLife = effect.FullLife;

        if (fullLife <= 0f) return 1f;

        var effectId = effect.EffectId;
        var isFresh = effect.LifeLeft == fullLife;

        if (_memo.TryRecall(effectId, isFresh, out var clock)) return clock;

        clock = ClockOfAction(effect.OwnersActionId());
        _memo.Remember(effectId, clock);

        return clock;
    }

    /// <summary>
    /// The time step a sped-up effect takes this frame. The game's payout timers pay at most once per frame, so a
    /// step longer than the shortest payout period would skip payouts: such a step is cut to the period and the
    /// action takes a little longer on that frame. The last step stops at the effect's end, so a payout due after
    /// the end never fires. A sped-up effect never steps slower than the game itself.
    /// </summary>
    /// <param name="longestStep">The shortest payout period in the game's time units; 0, less or not a number means unknown.</param>
    public static float Step(float gameStep, float clock, float longestStep, float lifeLeft)
    {
        var step = gameStep * clock;

        if (step <= gameStep) return step;

        var limit = Math.Min(longestStep > 0f ? longestStep : float.PositiveInfinity, lifeLeft);

        return Math.Max(gameStep, Math.Min(step, limit));
    }

    /// <summary>The game counts effect time in game seconds divided by the chapter's time scale.</summary>
    public static float LongestStep(int timeScale) => timeScale > 0 ? ShortestPayoutGameSeconds / timeScale : float.PositiveInfinity;

    private float ClockOfAction(int? actionId) => actionId is { } id && _clockByAction.TryGetValue(id, out var clock) ? clock : 1f;
}
