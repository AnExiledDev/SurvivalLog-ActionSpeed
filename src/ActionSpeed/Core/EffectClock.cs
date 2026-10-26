using System;
using System.Collections.Generic;

namespace ActionSpeed.Core;

/// <summary>Who an action's effect or progress bar belongs to. Each member reads the game's memory, so it is asked only when needed.</summary>
public interface IEffectOwner
{
    /// <summary>The owner's action is aimed at a container that has been searched before.</summary>
    bool IsOwnersTargetSearched();

    /// <summary>Who started the effect: the player, the robot, a survivor or anyone else.</summary>
    Character Owner();
}

/// <summary>One effect as the game's per-frame effect update hands it over. Each member reads the game's memory, so
/// <see cref="EffectClock"/> asks for as few as it can before it decides.</summary>
public interface IRunningEffect : IEffectOwner
{
    /// <summary>Started by an action, as opposed to a buff, a piece of furniture or the world.</summary>
    bool IsActionEffect { get; }

    /// <summary>The effect's whole life: -1 when it lasts until something ends it, 0 when it never counts down.</summary>
    float FullLife { get; }

    float LifeLeft { get; }

    /// <summary>The game gives this id to a later effect once this one ends.</summary>
    long EffectId { get; }

    /// <summary>The loaded game the effect runs in. Loading a save starts a new one, and its effect ids start over.</summary>
    long WorldId { get; }

    /// <summary>The action the effect's owner is doing now, or null when the owner or its action cannot be found.</summary>
    int? OwnersActionId();

    /// <summary>The chapter's time scale: game seconds per unit of effect time.</summary>
    int TimeScale();
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

    /// <summary>
    /// The clock of a search of a container already searched: fast enough to end within a few frames, since a
    /// search pays out only when it ends, and still finite so the progress bar shrinks with it.
    /// </summary>
    public const float Instant = 10000f;

    /// <summary>The game's effect that opens a container's bag when the action ends.</summary>
    public const int OpenBagEffect = 7;

    private IReadOnlyDictionary<int, float> _clockByAction = new Dictionary<int, float>();
    private IReadOnlySet<int> _searchActions = new HashSet<int>();
    private CharacterSpeeds _workSpeeds = new(1f, 1f, 1f);

    /// <summary>Nothing is sped up, slowed or free, so every effect keeps the game's clock without being read.</summary>
    private bool _isGameClockOnly = true;

    /// <summary>The loaded game the remembered clocks belong to.</summary>
    private long _worldId;

    /// <summary>The clock of every action that does not run on the game's.</summary>
    public void Publish(IReadOnlyDictionary<int, float> clockByAction)
    {
        _clockByAction = clockByAction;
        Settle();
    }

    /// <summary>A search waits its length and then opens the bag; tidying up opens one at once and lasts until closed.</summary>
    public static bool IsSearch(int effectId, float gameDuring) => effectId == OpenBagEffect && gameDuring > 0f;

    /// <summary>The actions that search a container, which end at once when the container was searched before.</summary>
    public void PublishSearches(IReadOnlySet<int> searchActions)
    {
        _searchActions = searchActions;
        Settle();
    }

    /// <summary>How fast the player, the robot and survivors do every action, on top of the action's own speed.</summary>
    public void PublishWorkSpeeds(CharacterSpeeds workSpeeds)
    {
        _workSpeeds = workSpeeds;
        Settle();
    }

    private void Settle() => _isGameClockOnly = _clockByAction.Count == 0 && _searchActions.Count == 0 && _workSpeeds.IsGamePace;

    /// <summary>Called every few seconds so effects that ended are forgotten.</summary>
    public void ForgetEnded() => _memo.Age();

    /// <summary>
    /// Only an effect an action started, timed by the action's length, runs on the action's clock. One that lasts
    /// until something ends it, or never counts down, has no length to finish sooner. An effect that has just
    /// started may carry the id of one that ended, so it is looked up again, and so is every effect after a save loads.
    /// </summary>
    public float ClockOf<TEffect>(TEffect effect) where TEffect : IRunningEffect
    {
        if (_isGameClockOnly || !effect.IsActionEffect) return 1f;

        var fullLife = effect.FullLife;

        if (fullLife <= 0f) return 1f;

        ForgetEarlierGame(effect.WorldId);

        var effectId = effect.EffectId;
        var isFresh = effect.LifeLeft == fullLife;

        if (_memo.TryRecall(effectId, isFresh, out var clock)) return clock;

        clock = ClockOfAction(effect.OwnersActionId(), effect);
        _memo.Remember(effectId, clock);

        return clock;
    }

    /// <summary>
    /// The time step the effect takes this frame, and the clock it runs on. A frame with no time passing (the game
    /// paused) reads nothing about the effect.
    /// </summary>
    public float StepOf<TEffect>(TEffect effect, float gameStep, out float clock) where TEffect : IRunningEffect
    {
        clock = 1f;

        if (gameStep == 0f) return gameStep;

        clock = ClockOf(effect);

        return clock == 1f ? gameStep : Step(gameStep, clock, LongestStep(effect.TimeScale()), effect.LifeLeft);
    }

    /// <summary>Effect ids start over in a newly loaded game, so a clock remembered from the earlier one would be wrong.</summary>
    private void ForgetEarlierGame(long worldId)
    {
        if (worldId == _worldId) return;

        _worldId = worldId;
        _memo.Clear();
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
    /// <summary>
    /// How long an action's progress bar should run. The bar is sized from the action's table length, which a
    /// sped-up effect finishes early, so the bar is shortened by the same clock to reach the end with the action.
    /// </summary>
    public static float BarSeconds(float during, float clock) =>
        clock > 0f && !float.IsNaN(clock) && !float.IsInfinity(clock) ? during / clock : during;

    public static float LongestStep(int timeScale) => timeScale > 0 ? ShortestPayoutGameSeconds / timeScale : float.PositiveInfinity;

    /// <summary>
    /// The clock an action's effect runs on: the action's own speed times the speed of whoever does it, and 1 when
    /// both keep the game's. Looking in a container a second time finds nothing new, so it costs no time, and no
    /// exposure on an expedition.
    /// </summary>
    /// <param name="owner">Asked whether the target was searched only for a search, and who it is only when someone
    /// works at other than the game's pace, since both read the game's memory.</param>
    public float ClockOfAction<TOwner>(int? actionId, TOwner owner) where TOwner : IEffectOwner
    {
        if (actionId is not { } id) return 1f;

        if (_searchActions.Contains(id) && owner.IsOwnersTargetSearched()) return Instant;

        var actionClock = _clockByAction.TryGetValue(id, out var clock) ? clock : 1f;

        return _workSpeeds.IsGamePace ? actionClock : actionClock * Walking.SpeedFor(owner.Owner(), _workSpeeds);
    }
}
