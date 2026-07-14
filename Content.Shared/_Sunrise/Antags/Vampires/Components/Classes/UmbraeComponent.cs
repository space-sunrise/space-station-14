using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[AutoGenerateComponentState]
public sealed partial class UmbraeComponent : Component
{
    [AutoNetworkedField]
    public bool CloakOfDarknessActive;

    [DataField]
    public int BreakLightBloodThreshold = 300;

    [DataField]
    public float BreakLightRange = 8f;

    [DataField]
    public float CloakOfDarknessRevealRange = 4.5f;

    [DataField]
    public float CloakOfDarknessMinVisibility = -0.8f;

    [DataField]
    public float CloakOfDarknessMaxVisibility = 0.6f;

    [DataField]
    public TimeSpan CloakOfDarknessVisibilityUpdateInterval = TimeSpan.FromSeconds(0.15);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextCloakOfDarknessVisibilityUpdate;

    public bool CloakHadStealthComponent;

    public bool CloakPreviousStealthEnabled;

    public float CloakPreviousStealthVisibility = 1f;

    [AutoNetworkedField]
    public bool EternalDarknessActive;
    public EntityUid? EternalDarknessAuraEntity;
    [AutoNetworkedField]
    public bool ShadowBoxingActive;

    [AutoNetworkedField]
    public EntityUid? ShadowBoxingTarget;
    public TimeSpan? ShadowBoxingEndTime;

    [AutoNetworkedField]
    public EntityUid? SpawnedShadowAnchorBeacon;

    [AutoPausedField]
    public TimeSpan? ShadowAnchorAutoReturnTime;

    public bool ShadowAnchorPlacementInProgress;
    public int ShadowAnchorLoopId;

    /// <summary>
    /// List of placed shadow snare traps
    /// </summary>
    public List<EntityUid> PlacedSnares = new();

    /// <summary>
    /// Maximum number of shadow snares that can be placed
    /// </summary>
    [DataField]
    public int MaxSnares = 3;
}
