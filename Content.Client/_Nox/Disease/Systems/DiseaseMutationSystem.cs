// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.Disease;
using Robust.Client.GameObjects;

namespace Content.Client._Nox.Disease.Systems;

public sealed class DiseaseMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseMutationComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }
    private void OnAppearanceChange(EntityUid uid, DiseaseMutationComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, DiseaseMutationVisuals.infected, out var infected, args.Component))
        {
            if (infected)
                _sprite.LayerSetRsiState(uid, 0, component.InfectedState);
            else
                _sprite.LayerSetRsiState(uid, 0, component.State);
        }
    }
}
