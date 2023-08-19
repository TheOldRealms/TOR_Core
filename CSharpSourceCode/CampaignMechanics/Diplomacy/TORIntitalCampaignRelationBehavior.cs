using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    public class TORIntitalCampaignRelationBehavior : CampaignBehaviorBase
    {
        private bool ticked;
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this,SetAliances);
        }

        private void SetAliances()
        {
            if(ticked) return;
            foreach (var kingdom in Campaign.Current.Kingdoms)
            {
                foreach (var otherkingdom in Campaign.Current.Kingdoms.Where(x=> x !=kingdom))
                {
                    foreach (var hero in kingdom.Heroes)
                    {
                        
                        foreach (var otherHero in otherkingdom.Heroes)
                        {
                            if (hero.GetDominantReligion() == null) continue;
                            if (otherHero.GetDominantReligion() == null) continue;
                            if (hero.GetDominantReligion().Name == otherHero.GetDominantReligion().Name)
                            {
                                if (hero.Culture == otherHero.Culture)
                                {
                                    hero.SetPersonalRelation(otherHero,50);
                                    continue;
                                }
                          
                            }

                            if (hero.GetDominantReligion().Affinity == ReligionAffinity.Order && otherHero.GetDominantReligion().Affinity == ReligionAffinity.Order)
                            {
                                hero.SetPersonalRelation(otherHero, 10);
                                continue;
                            }

                            if (hero.GetDominantReligion().HostileReligions.Contains(otherHero.GetDominantReligion()))
                            {
                                hero.SetPersonalRelation(otherHero,-25);
                                continue;
                            }
                            
                            hero.SetPersonalRelation(otherHero, 0);
                            
                            
                           
                            
                        }
                        
                    }
                }
            }

            ticked = true;
        }


        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}