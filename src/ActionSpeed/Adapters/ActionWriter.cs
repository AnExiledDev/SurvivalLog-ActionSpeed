using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionSpeed.Core;
using GameCore.HotUpdate;

namespace ActionSpeed.Adapters;

/// <summary>
/// Writes the chosen durations onto the game's action table, which every action reads its length from when it
/// starts. The table only exists once the game has loaded it, and a reload would bring back the game's durations,
/// so this runs on every sweep and rewrites what changed.
/// </summary>
internal sealed class ActionWriter
{
    private readonly SpeedSettings _settings;
    private readonly string _listPath;
    private readonly Action<string> _log;

    /// <summary>Each action's duration as the game first defined it, before this mod wrote over it.</summary>
    private readonly Dictionary<int, float> _gameDuring = new();

    private Dictionary<int, float> _planned = new();

    public ActionWriter(SpeedSettings settings, string listPath, Action<string> log)
    {
        _settings = settings;
        _listPath = listPath;
        _log = log;
    }

    public void Sweep()
    {
        var config = BaseSingleton<ConfigManager>.IsInstanceCreated ? BaseSingleton<ConfigManager>.Instance : null;
        var actions = config?._Config_Action_Dict;
        var effects = config?._Config_Effect_Dict;

        // Without the effect table every action with an effect would look unknown and be left alone, so wait for both.
        if (actions == null || effects == null || actions.Count == 0 || effects.Count == 0) return;

        var rows = new List<Config_Action>(actions.Count);

        foreach (var action in actions.Values)
        {
            if (action != null) rows.Add(action);
        }

        if (rows.Any(action => !_planned.ContainsKey(action.ID))) Plan(rows, effects);

        var written = rows.Count(Write);

        if (written > 0) _log($"the game reloaded its action table, {written} durations written again");
    }

    private void Plan(List<Config_Action> rows, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects)
    {
        var isFirstPlan = _planned.Count == 0;
        var intervals = IntervalsOf(rows, effects);
        var decided = rows.Select(action => GameRow(action, intervals)).Select(row => (Row: row, Decision: ActionSpeeds.Decide(row, _settings))).ToList();
        var changed = decided.Count(pair => pair.Decision.During != pair.Row.GameDuring);

        _planned = decided.ToDictionary(pair => pair.Row.Id, pair => pair.Decision.During);
        WriteList(decided.Select(pair => ActionSpeeds.Describe(pair.Row, pair.Decision, TypeName(pair.Row.Type))));

        _log(isFirstPlan
            ? $"{changed} of {rows.Count} actions changed, every action is listed in {Path.GetFileName(_listPath)}"
            : $"the game's action table gained actions, {changed} of {rows.Count} now changed");
    }

    /// <summary>The timed stat values of every effect an action uses; the table holds thousands the actions never touch.</summary>
    private static Dictionary<int, float[]> IntervalsOf(List<Config_Action> rows, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects)
    {
        var intervals = new Dictionary<int, float[]>();

        foreach (var effectId in rows.Select(action => action.EffectConfigID).Where(id => id > 0).Distinct())
        {
            if (effects.TryGetValue(effectId, out var effect) && effect != null) intervals[effectId] = TimedValues(effect);
        }

        return intervals;
    }

    private static float[] TimedValues(Config_Effect effect) => new[]
    {
        effect.ItemSatietyInterval, effect.ItemMoraleInterval, effect.ItemStaminaInterval, effect.ItemHealthInterval, effect.ItemVitalityInterval,
        effect.BuffSatietyInterval, effect.BuffMoraleInterval, effect.BuffStaminaInterval, effect.BuffHealthInterval, effect.BuffVitalityInterval,
    };

    private ActionRow GameRow(Config_Action action, Dictionary<int, float[]> intervals)
    {
        if (!_gameDuring.TryGetValue(action.ID, out var gameDuring)) _gameDuring[action.ID] = gameDuring = action.During;

        return new ActionRow(action.ID, DisplayName(action), action.ActionType, gameDuring, ActionSpeeds.PaysOverTime(action.EffectConfigID, intervals));
    }

    private bool Write(Config_Action action)
    {
        if (!_planned.TryGetValue(action.ID, out var during) || action.During == during) return false;

        action.During = during;

        return true;
    }

    private void WriteList(IEnumerable<string> lines)
    {
        try
        {
            File.WriteAllLines(_listPath, new[] { "id | name | type | game clock -> with this mod | why" }.Concat(lines));
        }
        catch (Exception ex)
        {
            _log($"could not write {Path.GetFileName(_listPath)}: {ex.Message}");
        }
    }

    /// <summary>The name the game shows over the character's head; the raw table text if it cannot be looked up.</summary>
    private static string DisplayName(Config_Action action)
    {
        var name = action.Name ?? "";

        try
        {
            var shown = ConstantTextTools.GetLocalText(name);

            return string.IsNullOrWhiteSpace(shown) ? name : shown;
        }
        catch (Exception)
        {
            return name;
        }
    }

    internal static string TypeName(int type) =>
        type == 0 ? "FurnitureUse" : Enum.IsDefined(typeof(ActionType), type) ? ((ActionType)type).ToString() : $"type {type}";
}
