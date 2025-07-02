using Robust.Shared.GameObjects;

namespace Content.Server.Sunrise.Sandevistan
{
    public sealed class SandevistanModule : IModule
    {
        public void Initialize()
        {
            IoCManager.Register<SandevistanSystem>();
        }
    }
}
