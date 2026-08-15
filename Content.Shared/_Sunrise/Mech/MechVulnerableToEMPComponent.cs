using Content.Shared.Damage;
using Content.Shared._Sunrise.Particles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Mech;

/// <summary>
/// Makes a mech vulnerable to electromagnetic pulses.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechVulnerableToEMPComponent : Component
{
    /// <summary>Server time at which another EMP pulse may affect this mech.</summary>
    [ViewVariables]
    public TimeSpan NextPulseTime;

    /// <summary>Duration for which one EMP hit keeps the periodic effect active.</summary>
    [DataField]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(6);

    /// <summary>Damage applied when the mech initially receives an EMP pulse.</summary>
    [DataField]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict = new()
        {
            { "Shock", 25f },
        }
    };

    /// <summary>
    /// Particle orchestra shown for every EMP damage pulse.
    /// </summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype> EmpParticleOrchestra = "EmpDisabledSparks";
}
