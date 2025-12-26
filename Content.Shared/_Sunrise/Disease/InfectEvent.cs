// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Content.Shared.Inventory;
using Content.Shared.Store.Components;
using Content.Shared.Store;
using Robust.Shared.GameObjects;
namespace Content.Shared._Sunrise.Disease;

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


public sealed partial class DiseaseAddSymptomEvent : InstantActionEvent
{
    [DataField] public string Symptom;
    [DataField] public int MinLevel = 0;
    [DataField] public int MaxLevel = 9999;
}


public sealed partial class DiseaseAddBaseChanceEvent : InstantActionEvent
{
}

public sealed partial class DiseaseAddCoughChanceEvent : InstantActionEvent
{
}

public sealed partial class DiseaseAddLethalEvent : InstantActionEvent
{
}

public sealed partial class DiseaseAddShieldEvent : InstantActionEvent
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

public sealed partial class DiseaseInfoEvent : InstantActionEvent
{
}

public sealed partial class DiseaseZombieEvent : InstantActionEvent
{
}







