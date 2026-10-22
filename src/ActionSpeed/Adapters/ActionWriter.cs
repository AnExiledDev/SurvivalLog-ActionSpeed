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
    private readonly string _listPath;
    private readonly Action<string> _log;
    private readonly Action<IReadOnlyDictionary<int, float>> _publishClocks;
    private readonly Action<IReadOnlySet<int>> _publishSearches;

    /// <summary>Each action's duration as the game first defined it, before this mod wrote over it.</summary>
    private readonly Dictionary<int, float> _gameDuring = new();

    private readonly ActionNames _names = new(LocalText);

    private Dictionary<int, float> _planned = new();
    private SpeedSettings? _settings;
    private bool _isReopenFree;

    public ActionWriter(string listPath, Action<string> log, Action<IReadOnlyDictionary<int, float>> publishClocks, Action<IReadOnlySet<int>> publishSearches)
    {
        _listPath = listPath;
        _log = log;
        _publishClocks = publishClocks;
        _publishSearches = publishSearches;
    }

    /// <summary>
    /// Called on the main thread at start and after each change. The next sweep plans every action again from the
    /// game's own durations and writes the new ones; an action already running keeps the length it started with.
    /// </summary>
    public void Use(SpeedSettings settings, bool isReopenFree)
    {
        _settings = settings;
        _isReopenFree = isReopenFree;
        _planned = new Dictionary<int, float>();
    }

    public void Sweep()
    {
        var settings = _settings;
        var config = BaseSingleton<ConfigManager>.IsInstanceCreated ? BaseSingleton<ConfigManager>.Instance : null;

        if (settings == null || config == null) return;

        var actions = config._Config_Action_Dict;

        if (actions == null || actions.Count == 0) return;

        var rows = new List<Config_Action>(actions.Count);

        foreach (var action in actions.Values)
        {
            if (action != null) rows.Add(action);
        }

        var hasNewActions = rows.Any(action => !_planned.ContainsKey(action.ID));

        if (hasNewActions) Plan(rows, settings, config);
        else if (_names.HasNewlyResolved()) WriteList(rows, settings, config);

        var written = rows.Count(Write);

        // A sweep that just planned is writing that plan for the first time, and the plan has already been logged.
        if (written > 0 && !hasNewActions) _log($"the game reloaded its action table, {written} durations written again");
    }

    private void Plan(List<Config_Action> rows, SpeedSettings settings, ConfigManager config)
    {
        var isFirstPlan = _planned.Count == 0;
        var decided = Decide(rows, settings, config);
        var changed = decided.Count(pair => pair.Decision.Lasts != pair.Row.GameDuring);

        _planned = decided.ToDictionary(pair => pair.Row.Id, pair => pair.Decision.During);
        _publishClocks(decided.Where(pair => pair.Decision.Clock != 1f).ToDictionary(pair => pair.Row.Id, pair => pair.Decision.Clock));
        _publishSearches(_isReopenFree ? rows.Where(action => EffectClock.IsSearch(action.EffectConfigID, _gameDuring[action.ID])).Select(action => action.ID).ToHashSet() : new HashSet<int>());
        WriteList(decided);

        _log(isFirstPlan
            ? $"{changed} of {rows.Count} actions changed, every action is listed in {Path.GetFileName(_listPath)}"
            : $"the game's action table gained actions, {changed} of {rows.Count} now changed");
    }

    private List<(ActionRow Row, ActionDecision Decision)> Decide(List<Config_Action> rows, SpeedSettings settings, ConfigManager config) =>
        rows.Select(action => GameRow(action, config)).Select(row => (Row: row, Decision: ActionSpeeds.Decide(row, settings))).ToList();

    /// <summary>
    /// The action as the game defines it: its own duration, captured the first time the action is seen so a speed
    /// is never applied on top of an earlier one, and what its group is sorted by.
    /// </summary>
    private ActionRow GameRow(Config_Action action, ConfigManager config)
    {
        if (!_gameDuring.TryGetValue(action.ID, out var gameDuring)) _gameDuring[action.ID] = gameDuring = action.During;

        var item = ItemOf(action.ID, config);

        return new ActionRow(action.ID, _names.Name(action.ID, action.Name ?? ""), action.ActionType, gameDuring,
            ActionSpeeds.IsReading(action.ShowConfigID, action.FmodPath), EffectName(action.EffectConfigID, config), item?.Category ?? 0, item?.SubCategory ?? 0);
    }

    private static string EffectName(int effectId, ConfigManager config)
    {
        var effects = config._Config_Effect_Dict;

        return effects != null && effects.TryGetValue(effectId, out var effect) && effect != null ? effect.ConfigName ?? "" : "";
    }

    private static Config_Item? ItemOf(int actionId, ConfigManager config)
    {
        var items = config._Config_Item_Dict;

        return actionId > ActionGroups.ItemActionBase && items != null && items.TryGetValue(actionId - ActionGroups.ItemActionBase, out var item) ? item : null;
    }

    private bool Write(Config_Action action)
    {
        if (!_planned.TryGetValue(action.ID, out var during) || action.During == during) return false;

        action.During = during;

        return true;
    }

    /// <summary>Written again once the game's language has loaded, so names first listed as raw text keys show as the game shows them.</summary>
    private void WriteList(List<Config_Action> rows, SpeedSettings settings, ConfigManager config) => WriteList(Decide(rows, settings, config));

    private void WriteList(List<(ActionRow Row, ActionDecision Decision)> decided)
    {
        var lines = decided.Select(pair => ActionSpeeds.Describe(pair.Row, pair.Decision));

        try
        {
            File.WriteAllLines(_listPath, new[] { "id | name | setting | game clock -> with this mod | why" }.Concat(lines));
        }
        catch (Exception ex)
        {
            _log($"could not write {Path.GetFileName(_listPath)}: {ex.Message}");
        }
    }

    /// <summary>The name the game shows over the character's head; null when it cannot be looked up yet.</summary>
    private static string? LocalText(string key)
    {
        try
        {
            return ConstantTextTools.GetLocalText(key);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
