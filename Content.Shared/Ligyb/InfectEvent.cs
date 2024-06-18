// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Store;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radiation.Events;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Ligyb;

public sealed partial class InfectEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed class InfectWithChanceEvent : EntityEventArgs, IInventoryRelayEvent
{
    // Whenever locational damage is a thing, this should just check only that bit of armour.
    public bool Handled = false;
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public readonly NetEntity Target;
    public readonly NetEntity Disease;
    public float Prob;

    public InfectWithChanceEvent(NetEntity target, NetEntity disease, float prob)
    {
        Prob = prob;
        Target = target;
        Disease = disease;
    }
}

[Serializable, NetSerializable]
public sealed partial class ClientInfectEvent : EntityEventArgs
{
    public NetEntity Infected { get; }
    public NetEntity Owner { get; }
    public ClientInfectEvent(NetEntity infected, NetEntity owner)
    {
        Infected = infected;
        Owner = owner;
    }
}

public sealed partial class DiseaseShopActionEvent : InstantActionEvent
{
}


[Serializable, NetSerializable]
public sealed partial class DiseaseBuyEvent : EntityEventArgs
{
    public readonly string BuyId;

    public DiseaseBuyEvent(string buyId = "Sus")
    {
        BuyId = buyId;
    }
}

[Serializable, NetSerializable]
public sealed partial class DiseaseStartCoughEvent : EntityEventArgs
{
}


[Serializable, NetSerializable]
public sealed partial class DiseaseStartSneezeEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseStartVomitEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseZombieEvent : EntityEventArgs
{
}


[Serializable, NetSerializable]
public sealed partial class DiseaseStartCryingEvent : EntityEventArgs
{
}



[Serializable, NetSerializable]
public sealed partial class DiseaseAddBaseChanceEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseAddCoughChanceEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseAddLethalEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseAddShieldEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseNarcolepsyEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseMutedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseSlownessEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseBleedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseBlindnessEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DiseaseInsultEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class UpdateInfectionsEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public UpdateInfectionsEvent(NetEntity id)
    {
        Uid = id;
    }
}





