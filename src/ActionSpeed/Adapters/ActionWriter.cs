using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ActionSpeed.Core;
using GameCore.HotUpdate;
using ActionTable = Il2CppSystem.Collections.Generic.Dictionary<int, GameCore.HotUpdate.Config_Action>;

namespace ActionSpeed.Adapters;

/// <summary>
/// Writes the chosen durations onto the game's action table, which every action reads its length from when it
/// starts, and hands each action's effect clock to the effect hook. The table only exists once the game has loaded
/// it, and a reload would bring back the game's durations, so each sweep checks whether the table was replaced, the
/// language changed or a few changed actions lost their duration, and only then reads the table again. That read
/// asks the game about every action, so it runs a slice per frame and writes the table once it is all read.
/// </summary>
internal sealed class ActionWriter
{
    private const string ListHeader = "id | name | setting | game clock -> with this mod | why";
    private const double SliceMilliseconds = 2.0;

    private static readonly long SliceTicks = (long)(SliceMilliseconds * Stopwatch.Frequency / 1000.0);

    private readonly string _listPath;
    private readonly Action<string> _log;
    private readonly Action<IReadOnlyDictionary<int, float>> _publishClocks;
    private readonly Action<IReadOnlySet<int>> _publishSearches;

    /// <summary>Each action's duration as the game first defined it, before this mod wrote over it.</summary>
    private readonly Dictionary<int, float> _gameDuring = new();

    private readonly ActionNames _names = new(LocalText);
    private readonly TableWatch _watch = new();

    /// <summary>Made once, so a sweep and a slice allocate no delegate.</summary>
    private readonly Func<int, float> _durationOf;
    private readonly Func<bool> _isSliceSpent;
    private readonly WaitCallback _writeLists;

    private Dictionary<int, float> _planned = new();

    private SpeedSettings? _settings;
    private bool _isReopenFree;

    /// <summary>The table a sweep is checking, for <see cref="DurationOf"/>; null outside a sweep.</summary>
    private ActionTable? _sweepTable;

    /// <summary>The read in progress: the job, what it has read so far, and what it was started for.</summary>
    private SlicedJob? _build;
    private List<ReadAction> _read = new();
    private TableStamp _buildStamp;
    private SpeedSettings? _buildSettings;
    private long _sliceEndsAt;

    /// <summary>The language the action list was last written in.</summary>
    private (long Map, int Count)? _listedLanguage;

    /// <summary>The newest list waiting for the worker, whether the worker is running, and a failure it kept for the main thread.</summary>
    private readonly object _listLock = new();
    private List<(ActionRow Row, ActionDecision Decision)>? _pendingList;
    private bool _isListWriting;
    private string? _listFailure;

    public ActionWriter(string listPath, Action<string> log, Action<IReadOnlyDictionary<int, float>> publishClocks, Action<IReadOnlySet<int>> publishSearches)
    {
        _listPath = listPath;
        _log = log;
        _publishClocks = publishClocks;
        _publishSearches = publishSearches;
        _durationOf = DurationOf;
        _isSliceSpent = IsSliceSpent;
        _writeLists = _ => WriteLists();
    }

    /// <summary>A read of the table is in progress; <see cref="Continue"/> carries it on each frame.</summary>
    public bool IsBuilding => _build != null;

    /// <summary>
    /// Called on the main thread at start and after each change. The next sweep plans every action again from the
    /// game's own durations and writes the new ones; an action already running keeps the length it started with.
    /// </summary>
    public void Use(SpeedSettings settings, bool isReopenFree)
    {
        _settings = settings;
        _isReopenFree = isReopenFree;
        _planned = new Dictionary<int, float>();
        _watch.Forget();
        _build = null;
    }

    public void Sweep()
    {
        ReportListFailure();

        var settings = _settings;
        var config = BaseSingleton<ConfigManager>.IsInstanceCreated ? BaseSingleton<ConfigManager>.Instance : null;

        if (settings == null || config == null || _build != null) return;

        var actions = config._Config_Action_Dict;

        if (actions == null || actions.Count == 0) return;

        var language = config._customCache_k__BackingField?._LanguageMap_k__BackingField;
        var stamp = new TableStamp(actions.Pointer.ToInt64(), actions.Count, language == null ? 0L : language.Pointer.ToInt64(), language?.Count ?? 0);

        _sweepTable = actions;

        try
        {
            if (!_watch.NeedsWrite(stamp, _durationOf)) return;
        }
        finally
        {
            _sweepTable = null;
        }

        _read = new List<ReadAction>(actions.Count);
        _buildStamp = stamp;
        _buildSettings = settings;
        _build = new SlicedJob(ReadSteps(actions, config, _read));
        Continue();
    }

    /// <summary>Reads the next slice of the table; once it is all read, plans and writes it in the same frame.</summary>
    public void Continue()
    {
        var build = _build;

        if (build == null) return;

        _sliceEndsAt = Stopwatch.GetTimestamp() + SliceTicks;

        bool isDone;

        try
        {
            isDone = build.Advance(_isSliceSpent);
        }
        catch (Exception)
        {
            _build = null;

            throw;
        }

        if (!isDone) return;

        _build = null;
        Finish(_read, _buildStamp, _buildSettings!);
        _read = new List<ReadAction>();
    }

    private bool IsSliceSpent() => Stopwatch.GetTimestamp() >= _sliceEndsAt;

    /// <summary>A watched action's duration in the table now; -1 when it is missing, so it counts as changed.</summary>
    private float DurationOf(int id) =>
        _sweepTable != null && _sweepTable.TryGetValue(id, out var action) && action != null ? action._During_k__BackingField : -1f;

    /// <summary>
    /// The table's rows are gathered in one step, so no step holds the game's collection open across frames, then
    /// each row is read in its own step: its name, its effect and its item ask the game several times each.
    /// </summary>
    private IEnumerable<bool> ReadSteps(ActionTable actions, ConfigManager config, List<ReadAction> read)
    {
        var rows = new List<Config_Action>(actions.Count);

        foreach (var action in actions.Values)
        {
            if (action != null) rows.Add(action);
        }

        var effects = config._Config_Effect_Dict;
        var items = config._Config_Item_Dict;

        yield return true;

        foreach (var action in rows)
        {
            read.Add(ReadRow(action, effects, items));

            yield return true;
        }
    }

    /// <summary>Plans when the table holds actions not planned yet, writes the planned durations, and lists every action when that changed.</summary>
    private void Finish(List<ReadAction> read, TableStamp stamp, SpeedSettings settings)
    {
        var hasNewActions = read.Any(action => !_planned.ContainsKey(action.Row.Id));
        var isNewLanguage = _listedLanguage is not { } listed || listed.Map != stamp.Language || listed.Count != stamp.LanguageCount;
        List<(ActionRow Row, ActionDecision Decision)>? decided = null;

        if (hasNewActions || isNewLanguage) decided = read.Select(action => (Row: action.Row, Decision: ActionSpeeds.Decide(action.Row, settings))).ToList();

        if (hasNewActions) Plan(read, decided!);

        var written = read.Count(Write);

        // A sweep that just planned is writing that plan for the first time, and the plan has already been logged.
        if (written > 0 && !hasNewActions) _log($"the game reloaded its action table, {written} durations written again");

        if (decided != null)
        {
            QueueList(decided);
            _listedLanguage = (stamp.Language, stamp.LanguageCount);
        }

        _watch.Written(stamp, _planned, _gameDuring);
    }

    private void Plan(List<ReadAction> read, List<(ActionRow Row, ActionDecision Decision)> decided)
    {
        var isFirstPlan = _planned.Count == 0;
        var changed = decided.Count(pair => pair.Decision.Lasts != pair.Row.GameDuring);

        _planned = decided.ToDictionary(pair => pair.Row.Id, pair => pair.Decision.During);
        _publishClocks(decided.Where(pair => pair.Decision.Clock != 1f).ToDictionary(pair => pair.Row.Id, pair => pair.Decision.Clock));
        _publishSearches(_isReopenFree ? read.Where(action => EffectClock.IsSearch(action.EffectId, action.Row.GameDuring)).Select(action => action.Row.Id).ToHashSet() : new HashSet<int>());

        _log(isFirstPlan
            ? $"{changed} of {read.Count} actions changed, every action is listed in {Path.GetFileName(_listPath)}"
            : $"the game's action table gained actions, {changed} of {read.Count} now changed");
    }

    /// <summary>
    /// The action as the game defines it: its own duration, captured the first time the action is seen so a speed
    /// is never applied on top of an earlier one, and what its group is sorted by.
    /// </summary>
    private ReadAction ReadRow(Config_Action action, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect>? effects,
        Il2CppSystem.Collections.Generic.Dictionary<int, Config_Item>? items)
    {
        var id = action.ID;

        if (!_gameDuring.TryGetValue(id, out var gameDuring)) _gameDuring[id] = gameDuring = action.During;

        var effectId = action.EffectConfigID;
        var item = id > ActionGroups.ItemActionBase && items != null && items.TryGetValue(id - ActionGroups.ItemActionBase, out var found) ? found : null;
        var effectName = effects != null && effects.TryGetValue(effectId, out var effect) && effect != null ? effect.ConfigName ?? "" : "";
        var row = new ActionRow(id, _names.Name(action.Name ?? ""), action.ActionType, gameDuring,
            ActionSpeeds.IsReading(action.ShowConfigID, action.FmodPath), effectName, item?.Category ?? 0, item?.SubCategory ?? 0);

        return new ReadAction(action, row, effectId);
    }

    /// <summary>Reads and writes the duration field itself: the game's property would cost a runtime call per action.</summary>
    private bool Write(ReadAction read)
    {
        if (!_planned.TryGetValue(read.Row.Id, out var during) || read.Action._During_k__BackingField == during) return false;

        read.Action._During_k__BackingField = during;

        return true;
    }

    /// <summary>The list is a few hundred lines of text, so it is built and written on a worker; only the newest one waiting is written.</summary>
    private void QueueList(List<(ActionRow Row, ActionDecision Decision)> decided)
    {
        lock (_listLock)
        {
            _pendingList = decided;

            if (_isListWriting) return;

            _isListWriting = true;
        }

        ThreadPool.QueueUserWorkItem(_writeLists);
    }

    /// <summary>Runs on a worker, which never logs: a failure is kept for the next sweep to report.</summary>
    private void WriteLists()
    {
        while (TakePendingList() is { } decided)
        {
            try
            {
                File.WriteAllLines(_listPath, new[] { ListHeader }.Concat(decided.Select(pair => ActionSpeeds.Describe(pair.Row, pair.Decision))));
            }
            catch (Exception ex)
            {
                lock (_listLock) _listFailure = ex.Message;
            }
        }
    }

    private List<(ActionRow Row, ActionDecision Decision)>? TakePendingList()
    {
        lock (_listLock)
        {
            var decided = _pendingList;

            _pendingList = null;

            if (decided == null) _isListWriting = false;

            return decided;
        }
    }

    private void ReportListFailure()
    {
        string? failure;

        lock (_listLock)
        {
            failure = _listFailure;
            _listFailure = null;
        }

        if (failure != null) _log($"could not write {Path.GetFileName(_listPath)}: {failure}");
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

    /// <summary>One row of the table as read: the game's object, to write its duration, the row as planned, and its effect.</summary>
    private readonly record struct ReadAction(Config_Action Action, ActionRow Row, int EffectId);
}
