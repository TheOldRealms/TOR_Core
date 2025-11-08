namespace TOR_Core.AbilitySystem.Crosshairs
{
    public class SelfCrosshair : AbilityCrosshair
    {
        private bool _isVisible = false;
        public SelfCrosshair(AbilityTemplate template) : base(template) { }

        public override void Dispose() => base.Dispose();

        public override bool IsVisible { get => _isVisible; protected set => _isVisible = value; }
    }
}
