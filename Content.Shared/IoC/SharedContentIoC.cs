using Content.Shared._Sunrise.BloodCult.Systems;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<MarkingManager, MarkingManager>();
            IoCManager.Register<ContentLocalizationManager, ContentLocalizationManager>();
            IoCManager.Register<CultistWordGeneratorManager, CultistWordGeneratorManager>(); // Sunrise-Edit
        }
    }
}
