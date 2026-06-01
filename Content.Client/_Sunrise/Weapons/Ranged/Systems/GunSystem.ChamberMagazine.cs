using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    partial void InitializeSunriseChamberMagazine()
    {
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, AfterAutoHandleStateEvent>(OnSunriseChamberMagazineState);
    }

    private void OnSunriseChamberMagazineState(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !_sprite.LayerMapTryGet((uid, sprite), GunVisualLayers.Base, out var boltLayer, false) ||
            !Appearance.TryGetData(uid, AmmoVisuals.BoltClosed, out bool boltClosed))
        {
            return;
        }

        var prefix = string.IsNullOrEmpty(component.SelectedPrefix) ? "" : $"_{component.SelectedPrefix}";
        _sprite.LayerSetRsiState((uid, sprite), boltLayer, boltClosed ? $"base{prefix}" : $"bolt-open{prefix}");
    }
}
