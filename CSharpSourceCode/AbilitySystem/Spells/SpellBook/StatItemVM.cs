﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.AbilitySystem.SpellBook
{
    public class StatItemVM : ViewModel
    {
        private string _lbl;
        private string _value;

        public StatItemVM(string labeltxt, string valuetxt)
        {
            _lbl = labeltxt;
            _value = valuetxt;
        }

        [DataSourceProperty]
        public string Label
        {
            get
            {
                return this._lbl;
            }
            set
            {
                if (value != this._lbl)
                {
                    this._lbl = value;
                    base.OnPropertyChangedWithValue(value, "Label");
                }
            }
        }

        [DataSourceProperty]
        public string Value
        {
            get
            {
                return this._value;
            }
            set
            {
                if (value != this._value)
                {
                    this._value = value;
                    base.OnPropertyChangedWithValue(value, "Value");
                }
            }
        }
    }
}
