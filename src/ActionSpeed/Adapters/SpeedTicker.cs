using System;
using UnityEngine;

namespace ActionSpeed.Adapters;

/// <summary>Every few real seconds, makes sure the action table carries the chosen durations and lets the effect hook forget ended effects.</summary>
public sealed class SpeedTicker : MonoBehaviour
{
    private const float SweepSeconds = 5f;
    private const int MaxLoggedFailures = 3;

    internal static ActionWriter? Writer;
    internal static Action<string> Log = _ => { };

    /// <summary>Starts half a period in, so this sweep and other mods' 5 second sweeps fall on different frames.</summary>
    private float _nextSweepAt = SweepSeconds / 2;
    private int _failures;

    public SpeedTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (Writer == null || Time.unscaledTime < _nextSweepAt) return;

        _nextSweepAt = Time.unscaledTime + SweepSeconds;

        try
        {
            EffectClockPatch.ForgetEnded();
            Writer.Sweep();
        }
        catch (Exception ex)
        {
            _failures++;

            if (_failures <= MaxLoggedFailures) Log($"writing action durations failed, tried again in {SweepSeconds:0}s (logged {_failures} of {MaxLoggedFailures}): {ex}");
        }
    }
}
