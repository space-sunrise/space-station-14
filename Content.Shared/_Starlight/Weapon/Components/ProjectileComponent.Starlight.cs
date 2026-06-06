using Content.Shared.Projectiles;
using Robust.Shared.GameStates;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Projectiles;

public sealed partial class ProjectileComponent
{
    [DataField]
    [AutoNetworkedField]
    public float ArmorPenetration = 0f;
}
