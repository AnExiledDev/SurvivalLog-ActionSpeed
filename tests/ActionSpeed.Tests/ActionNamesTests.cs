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
    public void A_name_asked_for_before_the_language_loads_shows_its_key()
    {
        var names = new ActionNames(new FakeLanguage().Look);

        Assert.Equal("Action_Name_112", names.Name("Action_Name_112"));
    }

    [Fact]
    public void A_name_asked_for_again_once_the_language_has_loaded_shows_the_games_text()
    {
        var language = new FakeLanguage();
        var names = new ActionNames(language.Look);

        names.Name("Action_Name_112");
        language.Load("Action_Name_112", "Use Toilet");

        Assert.Equal("Use Toilet", names.Name("Action_Name_112"));
    }

    [Fact]
    public void A_name_follows_a_change_of_language()
    {
        var language = new FakeLanguage();
        var names = new ActionNames(language.Look);

        language.Load("Action_Name_5", "Sleep");
        names.Name("Action_Name_5");
        language.Load("Action_Name_5", "Schlafen");

        Assert.Equal("Schlafen", names.Name("Action_Name_5"));
    }

    [Fact]
    public void An_action_with_no_name_key_is_never_looked_up()
    {
        var names = new ActionNames(_ => throw new InvalidOperationException("an empty key is never looked up"));

        Assert.Equal("", names.Name(""));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(null)]
    public void A_blank_text_from_the_game_counts_as_not_yet_loaded(string? text)
    {
        var names = new ActionNames(_ => text);

        Assert.Equal("Action_Name_7", names.Name("Action_Name_7"));
    }
}
