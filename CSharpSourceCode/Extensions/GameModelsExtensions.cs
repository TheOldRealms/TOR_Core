﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TOR_Core.Models;

namespace TOR_Core.Extensions
{
    public static class GameModelsExtensions
    {
        public static TORAbilityModel GetSpellcraftModel(this GameModels models)
        {
            var result = models.GetGameModels().FirstOrDefault(x => x.GetType() == typeof(TORAbilityModel));
            return result == null ? null : (TORAbilityModel)result;
        }
    }
}
