﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Extensions
{
    public static class TeamExtensions
    {
        public static List<Team> GetEnemyTeams(this Team team)
        {
            return Mission.Current.Teams.Where(x => x.IsEnemyOf(team)).ToList();
        }

        public static List<Team> GetAllyTeams(this Team team)
        {
            return Mission.Current.Teams.Where(x => x.IsFriendOf(team)).ToList();
        }
    }
}
