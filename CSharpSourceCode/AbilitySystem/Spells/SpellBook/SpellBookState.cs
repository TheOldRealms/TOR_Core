﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;

namespace TOR_Core.AbilitySystem.SpellBook
{
    public class SpellBookState : GameState
    {
        public override bool IsMenuState => true;

        public bool IsTrainerMode { get; internal set; } = false;
        public string TrainerCulture { get; internal set; } = "empire";

        public SpellBookState() { }
    }
}
