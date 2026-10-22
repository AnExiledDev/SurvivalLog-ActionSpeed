using ActionSpeed.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using BaseResident = GameCore.HotUpdate.Battle.Logic.DynamicNpc;

namespace ActionSpeed.Adapters;

/// <summary>Tells the player, the robot and the base's survivors apart by a character's instance id.</summary>
internal static class Characters
{
    /// <summary>With no world loaded nobody is known, and an unknown character keeps the game's pace.</summary>
    public static Character Of(long instanceId)
    {
        if (!BaseSingleton<BattleLogicWorld>.IsInstanceCreated) return Character.Other;

        var agents = BaseSingleton<BattleLogicWorld>.Instance._AgentManager;

        if (agents == null) return Character.Other;

        var isPlayer = instanceId == agents.LeadingRoleId;
        var resident = isPlayer ? null : agents.GetAgent(instanceId)?.TryCast<BaseResident>();

        return Walking.Classify(isPlayer, resident != null, resident?.IsRobot == true);
    }
}
