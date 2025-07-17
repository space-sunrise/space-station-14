using Content.Shared.Actions;
using Content.Shared.Communications;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Abductor;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorGizmoComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Target;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AbductorVictimComponent : Component
{
    [DataField("position"), AutoNetworkedField]
    public EntityCoordinates? Position;

    [DataField("organ"), AutoNetworkedField]
    public AbductorOrganType Organ = AbductorOrganType.None;

    [DataField]
    public TimeSpan? LastActivation;

    [ViewVariables]
    public TimeSpan TransformationTime = TimeSpan.FromSeconds(180);

    [ViewVariables]
    public SoundSpecifier Mew = new SoundPathSpecifier("/Audio/_Sunrise/Voice/Felinid/cat_mew2.ogg");

    [ViewVariables, AutoNetworkedField]
    public bool IsExperimentCompleted;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class AbductorOwoTransformatedComponent : Component
{
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorOrganComponent : Component
{
    [DataField("organ"), AutoNetworkedField]
    public AbductorOrganType Organ;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorScientistComponent : Component
{
    [DataField("position"), AutoNetworkedField]
    public EntityCoordinates? SpawnPosition;

    [DataField, AutoNetworkedField]
    public EntityUid? Console;

    [DataField, AutoNetworkedField]
    public EntityUid? Agent;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem))]
public sealed partial class AbductorExtractorComponent : Component
{
}
