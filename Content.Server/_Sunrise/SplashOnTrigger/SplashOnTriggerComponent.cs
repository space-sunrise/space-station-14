using Content.Shared.Chemistry.Components;

namespace Content.Server._Sunrise.SplashOnTrigger
{

    [RegisterComponent]
    internal sealed partial class SplashOnTriggerComponent : Component
    {
        [DataField("splashReagents")] public Solution SplashReagents = new()
        {
        };
    }
}
