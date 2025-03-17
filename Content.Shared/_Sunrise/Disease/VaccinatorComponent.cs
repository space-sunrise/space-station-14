// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
namespace Content.Shared.Chemistry.Components;

/// <summary>
/// This is used for an entity that uses <see cref="ReactionMixerComponent"/> to mix any container with a solution after a period of time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedVaccinatorSystem))]
public sealed partial class VaccinatorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId = "mixer";

    [DataField, AutoNetworkedField]
    public bool Mixing;

    /// <summary>
    /// How long it takes for mixing to occurs.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MixDuration;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MixTimeEnd;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? MixingSound;

    [ViewVariables]
    public Entity<AudioComponent>? MixingSoundEntity;

    /// <summary>
    /// The sound that's played when the scanner prints off a report.
    /// </summary>
    [DataField("soundPrint")]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// What the machine will print
    /// </summary>
    [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MachineOutput = "ForensicReportPaper";

    /// <summary>
    /// When will the scanner be ready to print again?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;

    /// <summary>
    /// How often can the scanner print out reports?
    /// </summary>
    [DataField("printCooldown")]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);
}
