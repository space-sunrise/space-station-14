using Content.Shared._Sunrise.HardsuitInjection.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Threading;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.HardsuitInjection.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InjectComponent : Component
{
    [DataField("toggleInjectionAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleInjectionAction = "ActionToggleInjection";

    [DataField("injectionAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string InjectionAction = "ActionInjection";


    [DataField]
    public SlotFlags RequiredFlags = SlotFlags.OUTERCLOTHING;

    [DataField]
    public string ContainerId = "beakerSlot";


    [DataField]
    public string VerbText = "hardsuitinjection-toggle";


    [DataField]
    public TimeSpan? Delay = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan? StripDelay = TimeSpan.FromSeconds(10);


    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");


    public EntityUid? ToggleInjectionActionEntity;

    public EntityUid? InjectionActionEntity;

    public ContainerSlot? Container;

    [AutoNetworkedField]
    public bool Locked = true;

    public TimeSpan OpenCloseDelay = TimeSpan.FromSeconds(3);

    public bool CanBeOpened = true;

    public bool AlwaysOpen = false;

    public bool AutoClose = false;

    public TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(10);

    [ViewVariables]
    public TimeSpan LastOpenTime;

    [ViewVariables]
    public CancellationTokenSource? AutoCloseCancelToken;

    [DataField]
    public TimeSpan AmpulaInsertDelay = TimeSpan.FromSeconds(2);


    [AutoNetworkedField]
    public float HighPressureMultiplier = 1;

    [AutoNetworkedField]
    public float LowPressureMultiplier = 1;

    [AutoNetworkedField]
    public float HeatingCoefficient = 1;

    [AutoNetworkedField]
    public float CoolingCoefficient = 1;
}
