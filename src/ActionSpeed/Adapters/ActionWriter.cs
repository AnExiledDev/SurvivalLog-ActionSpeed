using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionSpeed.Core;
using GameCore.HotUpdate;

namespace ActionSpeed.Adapters;

/// <summary>
/// Writes the chosen durations onto the game's action table, which every action reads its length from when it
/// starts, and hands each action's effect clock to the effect hook. The table only exists once the game has loaded
/// it, and a reload would bring back the game's durations, so this runs on every sweep and rewrites what changed.
/// </summary>
internal sealed class ActionWriter
{
    private readonly SpeedSettings _settings;
    private readonly string _listPath;
    private readonly Action<string> _log;
    private readonly Action<IReadOnlyDictionary<int, float>> _publishClocks;

    /// <summary>Each action's duration as the game first defined it, before this mod wrote over it.</summary>
    private readonly Dictionary<int, float> _gameDuring = new();

    private Dictionary<int, float> _planned = new();

    public ActionWriter(SpeedSettings settings, string listPath, Action<string> log, Action<IReadOnlyDictionary<int, float>> publishClocks)
    {
        _settings = settings;
        _listPath = listPath;
        _log = log;
        _publishClocks = publishClocks;
    }

    public void Sweep()
    {
        var config = BaseSingleton<ConfigManager>.IsInstanceCreated ? BaseSingleton<ConfigManager>.Instance : null;
        var actions = config?._Config_Action_Dict;

        if (actions == null || actions.Count == 0) return;

        var rows = new List<Config_Action>(actions.Count);

        foreach (var action in actions.Values)
        {
            if (action != null) rows.Add(action);
        }

        var hasNewActions = rows.Any(action => !_planned.ContainsKey(action.ID));

        if (hasNewActions) Plan(rows);

        var written = rows.Count(Write);

        // A sweep that just planned is writing that plan for the first time, and the plan has already been logged.
        if (written > 0 && !hasNewActions) _log($"the game reloaded its action table, {written} durations written again");
    }

    private void Plan(List<Config_Action> rows)
    {
        var isFirstPlan = _planned.Count == 0;
        var decided = rows.Select(GameRow).Select(row => (Row: row, Decision: ActionSpeeds.Decide(row, _settings))).ToList();
        var changed = decided.Count(pair => pair.Decision.Lasts != pair.Row.GameDuring);

        _planned = decided.ToDictionary(pair => pair.Row.Id, pair => pair.Decision.During);
        _publishClocks(decided.Where(pair => pair.Decision.Clock != 1f).ToDictionary(pair => pair.Row.Id, pair => pair.Decision.Clock));
        WriteList(decided.Select(pair => ActionSpeeds.Describe(pair.Row, pair.Decision, TypeName(pair.Row.Type))));

        _log(isFirstPlan
            ? $"{changed} of {rows.Count} actions changed, every action is listed in {Path.GetFileName(_listPath)}"
            : $"the game's action table gained actions, {changed} of {rows.Count} now changed");
    }

    /// <summary>The game's own duration, captured the first time the action is seen, so a speed is never applied on top of an earlier one.</summary>
    private ActionRow GameRow(Config_Action action)
    {
        if (!_gameDuring.TryGetValue(action.ID, out var gameDuring)) _gameDuring[action.ID] = gameDuring = action.During;

        return new ActionRow(action.ID, DisplayName(action), action.ActionType, gameDuring);
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
