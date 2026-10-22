using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

/// <summary>A stand-in for the game's text table, which hands back the key itself until the language has loaded.</summary>
internal sealed class FakeLanguage
{
    private readonly Dictionary<string, string> _text = new();

    public void Load(string key, string text) => _text[key] = text;

    public string? Look(string key) => _text.TryGetValue(key, out var text) ? text : key;
}

public class ActionNamesTests
{
    [Fact]
    public void A_name_looked_up_before_the_language_loads_is_shown_once_the_language_has_loaded()
    {
        var language = new FakeLanguage();
        var names = new ActionNames(language.Look);

        Assert.Equal("Action_Name_112", names.Name(112, "Action_Name_112"));
        Assert.False(names.HasNewlyResolved());

        language.Load("Action_Name_112", "Use Toilet");

        Assert.True(names.HasNewlyResolved());
        Assert.Equal("Use Toilet", names.Name(112, "Action_Name_112"));
        Assert.False(names.HasNewlyResolved());
    }

    [Fact]
    public void A_name_the_game_resolves_at_once_never_asks_for_the_list_to_be_written_again()
    {
        var language = new FakeLanguage();
        var names = new ActionNames(language.Look);

        language.Load("Action_Name_5", "Sleep");

        Assert.Equal("Sleep", names.Name(5, "Action_Name_5"));
        Assert.False(names.HasNewlyResolved());
    }

    [Fact]
    public void An_action_with_no_name_key_is_not_waited_on()
    {
        var names = new ActionNames(_ => throw new InvalidOperationException("an empty key is never looked up"));

        Assert.Equal("", names.Name(100020001, ""));
        Assert.False(names.HasNewlyResolved());
    }

    [Fact]
    public void A_blank_text_from_the_game_counts_as_not_yet_loaded()
    {
        var language = new FakeLanguage();
        var names = new ActionNames(language.Look);

        language.Load("Action_Name_7", " ");

        Assert.Equal("Action_Name_7", names.Name(7, "Action_Name_7"));

        language.Load("Action_Name_7", "Nap");

        Assert.True(names.HasNewlyResolved());
    }
}
