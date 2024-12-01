using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem))]
public sealed partial class HitScanSpreadComponent : Component
{
    [DataField]
    public Angle Spread = Angle.FromDegrees(5);

    [DataField]
    public int Count = 1;
}
