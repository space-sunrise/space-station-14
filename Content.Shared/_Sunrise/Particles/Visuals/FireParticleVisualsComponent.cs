using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Configures the orchestra and emission geometry of fire particles for an entity.
/// </summary>
[RegisterComponent]
public sealed partial class FireParticleVisualsComponent : Component
{
    /// <summary>Optional orchestra replacing the default fire and smoke presentation.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype>? Orchestra;

    /// <summary>Local offset of the particle source from the sprite center.</summary>
    [DataField]
    public Vector2 Offset;

    /// <summary>Whether the emission source should cover the current visual sprite bounds.</summary>
    [DataField]
    public bool FillSprite = true;
}
