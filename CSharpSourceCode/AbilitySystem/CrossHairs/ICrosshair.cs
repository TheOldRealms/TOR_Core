namespace TOR_Core.AbilitySystem.CrossHairs
{
    public interface ICrosshair
    {
        void Show();
        void Hide();
        void Tick();

        bool IsVisible { get; }
    }
}
