using System;
using UnityEngine;

namespace ActionSpeed.Adapters;

/// <summary>
/// Every few real seconds, makes sure the action table carries the chosen durations and lets the effect hook forget
/// ended effects. A read of the table, once started, goes on a slice every frame until it is done.
/// </summary>
public sealed class SpeedTicker : MonoBehaviour
{
    private const long SweepMilliseconds = 5000;
    private const int MaxLoggedFailures = 3;

    internal static ActionWriter? Writer;
    internal static Action<string> Log = _ => { };

    /// <summary>
    /// Starts half a period after load, so this sweep and other mods' 5 second sweeps fall on different frames. The
    /// system clock is read without calling into Unity, since most frames have nothing to do.
    /// </summary>
    private long _nextSweepAt = Environment.TickCount64 + SweepMilliseconds / 2;
    private int _failures;

    public SpeedTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        var writer = Writer;

        if (writer == null) return;

        if (writer.IsBuilding)
        {
            try
            {
                writer.Continue();
            }
            catch (Exception ex)
            {
                Report(ex);
            }

            return;
        }

        var now = Environment.TickCount64;

        if (now < _nextSweepAt) return;

        _nextSweepAt = now + SweepMilliseconds;

        try
        {
            EffectClockPatch.ForgetEnded();
            writer.Sweep();
        }
        catch (Exception ex)
        {
            Report(ex);
        }
    }

    private void Report(Exception ex)
    {
        _failures++;

        if (_failures <= MaxLoggedFailures) Log($"writing action durations failed, tried again in {SweepMilliseconds / 1000}s (logged {_failures} of {MaxLoggedFailures}): {ex}");
    }
}
