using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShadowSnareComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 20f }
        }
    };

    [DataField, AutoNetworkedField]
    public float BlindDuration = 20f;

    /// <summary>
    /// Радиус гашения ближайших источников света
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightExtinguishRadius = 5f;

    /// <summary>
    /// Множитель скорости ходьбы для эффекта ловушки
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WalkSpeed = 0.4f;

    /// <summary>
    /// Множитель скорости спринта для эффекта ловушки
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprintSpeed = 0.4f;

    /// <summary>
    /// Время, за которое другой персонаж может освободить пойманную цель
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FreeTime = 3f;

    /// <summary>
    /// Время, за которое цель может освободиться сама
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BreakoutTime = 8f;

    [DataField]
    public SoundSpecifier TriggerSound = new SoundPathSpecifier("/Audio/Effects/snap.ogg");

    [DataField]
    public EntProtoId EnsnarePrototype = "VampireShadowSnareEnsnare";
}
