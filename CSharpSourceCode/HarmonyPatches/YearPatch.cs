namespace TOR_Core.Models.CustomBattleModels
{
    public class YearPatch
    {
        static AccessTools.FieldRef<MapEventParty, FlattenedTroopRoster> currentRoster =
            AccessTools.FieldRefAccess<MapEventParty, FlattenedTroopRoster>("_roster");
    }
}