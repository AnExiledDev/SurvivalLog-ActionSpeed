using System.Collections.Generic;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>A kind of action with its own speed setting. Every timed action belongs to exactly one.</summary>
public enum Group
{
    Meals,
    Snacks,
    Drinks,
    Medicine,
    Sickness,
    Moods,
    Toilet,
    Washing,
    Sleep,
    Nap,
    Reading,
    Games,
    Music,
    Dancing,
    QuietTime,
    Relaxing,
    Workouts,
    Cleaning,
    Organizing,
    BoilingWater,
    Power,
    Unpacking,
    Salvaging,
    Planting,
    Watering,
    Harvesting,
    Tending,
    Repairs,
    Reinforcing,
    Furnishing,
    Demolishing,
    Projects,
    Searching,
    BreakingIn,
    Inspecting,
    Drones,
    Traps,
    Throwing,
    Other,
}

/// <param name="Name">The config section, which holds the umbrella speed and one speed per group.</param>
public sealed record Section(string Name, IReadOnlyList<Group> Groups);

/// <summary>
/// Sorts the game's actions into groups. The game's own ActionType lumps unlike work together (drug items are food,
/// opening a furniture package is the same type as salvaging a trinket), so the groups come from what the action
/// is: its id for the game's fixed actions, its effect for families that share one, and the item's category for
/// actions that use an item. Anything not placed by those falls back by its game type.
/// </summary>
public static class ActionGroups
{
    /// <summary>The section of <see cref="Group.Other"/>, which sits directly under the everything speed.</summary>
    public const string ActionsSection = "Actions";

    /// <summary>Item actions share an id with their item, offset by this.</summary>
    public const int ItemActionBase = 100000000;

    public static readonly IReadOnlyList<Section> Sections = new[]
    {
        new Section("Eating", new[] { Group.Meals, Group.Snacks, Group.Drinks }),
        new Section("Health", new[] { Group.Medicine, Group.Sickness, Group.Moods }),
        new Section("Hygiene", new[] { Group.Toilet, Group.Washing }),
        new Section("Rest", new[] { Group.Sleep, Group.Nap }),
        new Section("Leisure", new[] { Group.Reading, Group.Games, Group.Music, Group.Dancing, Group.QuietTime, Group.Relaxing, Group.Workouts }),
        new Section("Chores", new[] { Group.Cleaning, Group.Organizing, Group.BoilingWater, Group.Power, Group.Unpacking, Group.Salvaging }),
        new Section("Farming", new[] { Group.Planting, Group.Watering, Group.Harvesting, Group.Tending }),
        new Section("Building", new[] { Group.Repairs, Group.Reinforcing, Group.Furnishing, Group.Demolishing, Group.Projects }),
        new Section("Scavenging", new[] { Group.Searching, Group.BreakingIn, Group.Inspecting, Group.Drones }),
        new Section("Defense", new[] { Group.Traps, Group.Throwing }),
    };

    private const int Food = 1;
    private const int Drug = 2;
    private const int Appliance = 4;
    private const int Fertilizer = 11;
    private const int Trap = 12;
    private const int Flower = 15;
    private const int Throwable = 16;
    private const int StoryItem = 18;
    private const int Collectible = 19;

    private const int Fruit = 6;
    private const int Snack = 7;
    private const int SoftDrink = 9;
    private const int Liquor = 10;

    private const int FurnitureUse = 0;

    private static readonly Dictionary<int, Group> ById = Index(new Dictionary<Group, int[]>
    {
        [Group.Meals] = new[] { 201, 251, 9064 },
        [Group.Medicine] = new[] { 255, Item(20214) },
        [Group.Sickness] = new[] { 133, 134 },
        [Group.Moods] = new[] { 8, 9, 10, 108, 113, 1620 },
        [Group.Toilet] = new[] { 112, 136, 1907, 9075 },
        [Group.Washing] = new[] { 103, 104, 107, 1915, 9085, Item(11009), Item(11010), Item(11011), Item(11012), Item(11013) },
        [Group.Sleep] = new[] { 5, 51, 1904, 1941, 8000 },
        [Group.Nap] = new[] { 7, 106, 200, 1105, 1905, 8001 },
        [Group.Reading] = new[] { 116 },
        [Group.Games] = new[] { 101, 111, 126, 252, 1908, 1909, 1913, Item(2508), Item(20207), Item(20208), Item(11006) },
        [Group.Music] = new[] { 21, 32, 105, 9066, 9067, 9068, 9076, 9089, 9090, 9094, 9095 },
        [Group.Dancing] = new[] { 9077, 9078, 9079, 9080 },
        [Group.QuietTime] = new[] { 20, 22, 24, 31, 35, 114 },
        [Group.Relaxing] = new[] { 143, 250, 253, 1910, 1911, 1912, 1914, 1950, 1951, 1952, 9063, 9082, Item(31001), Item(20221), Item(20213), Item(20215), Item(20217) },
        [Group.Workouts] = new[] { 6, 23, 100, 1916, Item(11001) },
        [Group.Cleaning] = new[] { 254, 1623, 9059, 9072 },
        [Group.Organizing] = new[] { 11, 115, 117, 9098, 9099, 9100, 9131, 9140, 9141 },
        [Group.BoilingWater] = new[] { 1903 },
        [Group.Power] = new[] { 1900, 1901, 1902, 1933 },
        [Group.Salvaging] = new[] { Item(24116), Item(24123) },
        [Group.Planting] = new[] { 1611, 9130, Item(24115), Item(20107) },
        [Group.Watering] = new[] { 102, 1615 },
        [Group.Harvesting] = new[] { 1617, 1624, 1625 },
        [Group.Tending] = new[] { 1613, 1614, 1616, 1618, 1619, 1920, 9084 },
        [Group.Repairs] = new[] { 25, 30, 43, 44, 120, 121, 122, 123, 1923, 1924, 1934, 1937, 9073, 9086, 9104, 9109, 9121, 9122, 9127, 9128, 9129 },
        [Group.Reinforcing] = new[] { 58, 62, 118, 124, 125, 132, 135, 214, 9071, 9096, 9097 },
        [Group.Furnishing] = new[] { 197, 198, 204, 1607 },
        [Group.Demolishing] = new[] { 45, 127, 128, 129, 130, 131, 199, 205, 1943 },
        [Group.Projects] = new[] { 36, 38, 42, 50, 55, 56, 57, 59, 60, 61, 64, 66, 67, 137, 207, 208, 209, 211, 212, 213, 215, 1921, 1922, 9118, 9119, 9123, 9124, 9125, 9126 },
        [Group.Searching] = new[] { 53, 109, 110, 1100, 1101, 1106, 1506, 1621, 1919, 1925, 1931, 1936, 9083 },
        [Group.BreakingIn] = new[] { 65, 202, 203, 256, 257, 258, 259, 1102, 1103, 1501, 1502, 1926, 1927, 1928, 1929, 1930, 1945, Item(9045), Item(9046), Item(9069), Item(9070) },
        [Group.Inspecting] = new[] { 40, 1104, 1107, 1503, 1504, 1505, 1601, 1602, 1603, 1604, 1605, 1606, 1932, Item(9003), Item(9089), Item(42047), Item(9073) },
        [Group.Drones] = new[] { 141, 9056, 9065, 9074, 9092, 9103 },
        [Group.Traps] = new[] { 63, 1800, 1803, 1805 },
    });

    private static readonly Dictionary<string, Group> ByEffect = new()
    {
        ["Action/Item_Generate_BagFurniture"] = Group.Unpacking,
        ["Action/Gift_Bar"] = Group.Unpacking,
        ["Action/Install_Trap"] = Group.Traps,
        ["Action/Dismantle_Trap"] = Group.Traps,
        ["Action/Move_Trap"] = Group.Traps,
        ["Action/Throw_Molotov"] = Group.Throwing,
        ["Action/Throw_ZombieLure"] = Group.Throwing,
        ["Action/Use_LightProp"] = Group.Throwing,
    };

    private static readonly Dictionary<int, Group> ByItemCategory = new()
    {
        [Drug] = Group.Medicine,
        [Appliance] = Group.Games,
        [Fertilizer] = Group.Tending,
        [Trap] = Group.Traps,
        [Flower] = Group.Relaxing,
        [Throwable] = Group.Throwing,
    };

    private static readonly Dictionary<int, Group> ByGameType = new()
    {
        [1] = Group.Meals,
        [2] = Group.Nap,
        [3] = Group.Relaxing,
        [4] = Group.Workouts,
        [5] = Group.QuietTime,
        [6] = Group.Medicine,
        [10] = Group.Repairs,
    };

    private static readonly Dictionary<Group, string> SectionByGroup = Sections
        .SelectMany(section => section.Groups.Select(group => (Group: group, Section: section.Name)))
        .Append((Group: Group.Other, Section: ActionsSection))
        .ToDictionary(pair => pair.Group, pair => pair.Section);

    /// <summary>Reading comes first because books are spread over several game types and items.</summary>
    public static Group Of(ActionRow row)
    {
        if (row.IsReading) return Group.Reading;

        if (ById.TryGetValue(row.Id, out var fixedGroup)) return fixedGroup;

        if (ByEffect.TryGetValue(row.Effect, out var effectGroup)) return effectGroup;

        if (row.ItemCategory != 0 && OfItem(row) is { } itemGroup) return itemGroup;

        return ByGameType.TryGetValue(row.Type, out var typeGroup) ? typeGroup : Group.Other;
    }

    public static string SectionOf(Group group) => SectionByGroup[group];

    /// <summary>How the action list names a group: its section and its setting.</summary>
    public static string Label(Group group) => $"{SectionOf(group)}/{group}";

    private static Group? OfItem(ActionRow row)
    {
        if (row.ItemSubCategory is SoftDrink or Liquor) return Group.Drinks;

        if (row.ItemCategory == Food) return row.ItemSubCategory is Snack or Fruit ? Group.Snacks : Group.Meals;

        if (row.ItemCategory is StoryItem or Collectible && row.Type == FurnitureUse) return Group.Salvaging;

        return ByItemCategory.TryGetValue(row.ItemCategory, out var group) ? group : null;
    }

    private static int Item(int itemId) => ItemActionBase + itemId;

    private static Dictionary<int, Group> Index(Dictionary<Group, int[]> idsByGroup) =>
        idsByGroup.SelectMany(pair => pair.Value.Select(id => (Id: id, Group: pair.Key))).ToDictionary(pair => pair.Id, pair => pair.Group);
}
