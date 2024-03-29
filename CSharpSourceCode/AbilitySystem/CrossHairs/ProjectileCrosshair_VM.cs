﻿using TaleWorlds.Library;

namespace TOR_Core.AbilitySystem.Crosshairs
{
    public class ProjectileCrosshair_VM : ViewModel
    {
        private string _name = "Projectile Crosshair";
        private string _spriteName = "test_spell_crosshair";
        private bool _isVisible;

        [DataSourceProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    base.OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get
            {
                return _spriteName;
            }
            set
            {
                if (value != _spriteName)
                {
                    _spriteName = value;
                    base.OnPropertyChangedWithValue(value, "SpriteName");
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                base.OnPropertyChangedWithValue(value, "IsVisible");
            }
        }
    }
}
