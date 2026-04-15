using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static Ink.Parsed.FlowBase;

namespace TOR_Core.Items.InventoryUseScripts
{
    /// <summary>
    /// Script to handle the use of skill books.
    /// </summary>
    /// <remarks>
    /// Expected arguments: SkillId, XP amount, learning time in hours
    /// </remarks>
    /// <param name="arguments"></param>
    public class SkillBookScript : BaseInventoryUseScript
    {
        [SaveableField(1)]
        private string _skillId;
        [SaveableField(2)]
        private int _xpAmount;
        [SaveableField(3)]
        private int _learningTime;
        [SaveableField(4)]
        private int _elapsedHours;

        public SkillBookScript(string[] arguments) : base(arguments)
        {
            if (arguments.Count() >= 3)
            {
                _skillId = arguments[0];
                if (Campaign.Current.ObjectManager.GetObject<SkillObject>(_skillId) == null)
                {
                    throw new ArgumentException($"SkillBookScript failed to find skill with ID: {_skillId}.");
                }
                if (!int.TryParse(arguments[1], out _xpAmount))
                {
                    throw new ArgumentException($"SkillBookScript failed to parse XP amount from argument: {arguments[1]}.");
                }
                if (!int.TryParse(arguments[2], out _learningTime))
                {
                    throw new ArgumentException($"SkillBookScript failed to parse learning time from argument: {arguments[2]}.");
                }
            }
            else
            {
                throw new ArgumentException("SkillBookScript requires at least 3 arguments: SkillId, XP amount, and learning time in hours.");
            }
            _elapsedHours = 0;
        }

        public override void OnDailyTick(MobileParty party) { }

        public override void OnHourlyTick(MobileParty party)
        {
            _elapsedHours++;
            if (_elapsedHours > _learningTime)
            {
                InventoryUseScriptsCampaignBehavior.Instance.RemoveScriptFromParty(party, this);
                TORCommon.Say($"Finished reading skill book for {_skillId}. Gained {_xpAmount} XP in total.");
            }
            else
            {
                SkillObject skill = Campaign.Current.ObjectManager.GetObject<SkillObject>(_skillId);
                if (skill != null)
                {
                    party.LeaderHero?.AddSkillXp(skill, _xpAmount / _learningTime);
                    TORCommon.Say($"Gained {_xpAmount / _learningTime} XP in {skill.Name}.");
                }
            }
        }

        public override void OnUse(MobileParty userParty, ItemObject item)
        {
            // Count how many learnable traits this skill book has
            int traitCount = item.GetTraits()?.Count ?? 1;
            int maxUsages = traitCount > 1 ? traitCount : 1; // Allow multiple uses if more than 1 trait

            var usageData = InventoryUseScriptsCampaignBehavior.Instance._usages.WhereQ(x => x.heroId == userParty.LeaderHero.StringId && x.itemId == item.StringId).FirstOrDefault();

            if (usageData != null && usageData.usages >= maxUsages)
            {
                TORCommon.Say(item.Name + " has already been used " + maxUsages + " time(s) by " + userParty.LeaderHero.Name + ".");
            }
            else if (InventoryUseScriptsCampaignBehavior.Instance.TryAddScriptToParty(userParty, this))
            {
                TORCommon.Say($"Started reading skill book for {_skillId}.");
            }
            else
            {
                TORCommon.Say("Already reading a skill book. Finish it before starting a new one.");
            }
        }
    }
}