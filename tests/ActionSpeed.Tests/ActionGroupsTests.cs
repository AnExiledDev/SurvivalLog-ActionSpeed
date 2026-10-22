using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class ActionGroupsTests
{
    private const int FurnitureUse = 0;
    private const int FoodType = 1;
    private const int RestType = 2;
    private const int OtherType = 8;

    private const int FoodItem = 1;
    private const int DrugItem = 2;
    private const int CollectibleItem = 19;
    private const int Fruit = 6;
    private const int Snack = 7;
    private const int SoftDrink = 9;
    private const int Meat = 2;

    private static Group Of(int id, int type, bool isReading = false, string effect = "", int category = 0, int subCategory = 0) =>
        ActionGroups.Of(new ActionRow(id, "action", type, 600f, isReading, effect, category, subCategory));

    [Fact]
    public void Reading_wins_over_every_other_rule_because_books_are_spread_over_many_types_and_items()
    {
        Assert.Equal(Group.Reading, Of(100002506, FoodType, isReading: true, category: FoodItem));
        Assert.Equal(Group.Reading, Of(112, OtherType, isReading: true));
    }

    [Fact]
    public void A_fixed_action_is_placed_by_its_id_whatever_its_game_type()
    {
        Assert.Equal(Group.Toilet, Of(112, OtherType));
        Assert.Equal(Group.Sleep, Of(5, RestType));
        Assert.Equal(Group.Cleaning, Of(254, RestType));
    }

    [Fact]
    public void A_family_sharing_one_effect_is_placed_by_that_effect()
    {
        Assert.Equal(Group.Unpacking, Of(100030001, FurnitureUse, effect: "Action/Item_Generate_BagFurniture"));
        Assert.Equal(Group.Traps, Of(100060001, OtherType, effect: "Action/Install_Trap"));
        Assert.Equal(Group.Throwing, Of(100070001, OtherType, effect: "Action/Throw_Molotov"));
    }

    [Theory]
    [InlineData(FoodItem, Snack, Group.Snacks)]
    [InlineData(FoodItem, Fruit, Group.Snacks)]
    [InlineData(FoodItem, Meat, Group.Meals)]
    [InlineData(FoodItem, SoftDrink, Group.Drinks)]
    [InlineData(DrugItem, 0, Group.Medicine)]
    public void An_item_action_is_placed_by_what_the_item_is(int category, int subCategory, Group expected)
    {
        Assert.Equal(expected, Of(100050001, FoodType, category: category, subCategory: subCategory));
    }

    [Fact]
    public void Taking_a_collectible_apart_is_salvaging_but_using_it_another_way_falls_back_to_its_type()
    {
        Assert.Equal(Group.Salvaging, Of(100024001, FurnitureUse, category: CollectibleItem));
        Assert.Equal(Group.Relaxing, Of(100024002, 3, category: CollectibleItem));
    }

    [Fact]
    public void An_action_no_rule_places_falls_back_by_its_game_type_and_else_to_other()
    {
        Assert.Equal(Group.Nap, Of(2999, RestType));
        Assert.Equal(Group.Meals, Of(2999, FoodType));
        Assert.Equal(Group.Other, Of(2999, OtherType));
        Assert.Equal(Group.Other, Of(2999, FurnitureUse));
    }

    [Fact]
    public void Every_group_has_one_setting_in_exactly_one_section()
    {
        var placed = ActionGroups.Sections.SelectMany(section => section.Groups).Append(Group.Other).ToList();

        Assert.Equal(Enum.GetValues<Group>().OrderBy(group => group), placed.OrderBy(group => group));
        Assert.Equal(ActionGroups.ActionsSection, ActionGroups.SectionOf(Group.Other));
        Assert.Equal("Leisure/Reading", ActionGroups.Label(Group.Reading));
    }
}
