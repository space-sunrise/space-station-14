using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Robust.Shared.GameObjects;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Mech;

public sealed partial class MechSystem
{
    partial void UpdateSunrisePaintAppearance(EntityUid uid, MechComponent component)
    {
        if (_appearance.TryGetData<string>(uid, MechVisualLayers.Siren, out var siren))
            component.SirenState = siren;
    }
}
