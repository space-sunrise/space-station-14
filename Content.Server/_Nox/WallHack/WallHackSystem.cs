// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Sunrise.WallHack.Components;

namespace Content.Server._Sunrise.WallHack;

public sealed partial class WallHackSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WallHackComponent, ComponentInit>((uid, comp, args) =>
        {
            if (EntityManager.TryGetComponent<EyeComponent>(uid, out var eyeComp))
                _eye.SetDrawLight((uid, eyeComp), false);
        });

        SubscribeLocalEvent<WallHackComponent, ComponentShutdown>((uid, comp, args) =>
        {
            if (EntityManager.TryGetComponent<EyeComponent>(uid, out var eyeComp))
                _eye.SetDrawLight((uid, eyeComp), true);
        });
    }
}
