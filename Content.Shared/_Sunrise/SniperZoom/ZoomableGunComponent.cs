using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.SniperZoom;

/// <summary>
/// Добавляйте этот компонент снайперкам.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ZoomableGunComponent : Component
{
    [DataField] public bool Enabled;
    [DataField] public string TakeAimAction = "ActionTakeAim";
    [DataField] public EntityUid? TakeAimActionEntity;
    [DataField] public Vector2 TargetZoom = new (1.25f, 1.25f);
    [DataField] public Vector2? Zoom;
    [DataField] public bool Wielded;
    [DataField] public float? BaseWalkSpeed;
    [DataField] public float? BaseSprintSpeed;
    [DataField] public float? BaseAcceleration;
    [DataField] public float TargetWalkSpeed = 2f;
    [DataField] public float TargetSprintSpeed = 3f;
    [DataField] public float TargetAcceleration = 20f;
}
