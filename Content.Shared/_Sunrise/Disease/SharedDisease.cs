// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.Inventory;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Content.Shared.Actions;
using Content.Shared._Sunrise.Disease.Components;

namespace Content.Shared._Sunrise.Disease;

/// <summary>
///     Логика резистов зомби инфекции.
/// </summary>
public sealed class DiseaseResistanceQueryEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; }
    public float TotalCoefficient = 0f;

    public DiseaseResistanceQueryEvent(SlotFlags slots)
    {
        TargetSlots = slots;
    }
}

public sealed class CureDiseaseEvent : EntityEventArgs
{
    public EntityUid Target { get; }

    public CureDiseaseEvent(EntityUid target)
    {
        Target = target;
    }
}

public sealed class ProbInfectAttemptEvent : EntityEventArgs
{
    public EntityUid Target { get; }
    public EntityUid? Host { get; }
    public bool Cancel { get; set; }

    public ProbInfectAttemptEvent(EntityUid target, bool cancel = false, EntityUid? host = null)
    {
        Target = target;
        Host = host;
        Cancel = cancel;
    }
}

public sealed class CauseDiseaseEvent : EntityEventArgs
{
    public DiseaseData SourceData { get; }

    public CauseDiseaseEvent(DiseaseData sourceData)
    {
        SourceData = sourceData;
    }
}

public sealed class EnterCryostorageEvent : EntityEventArgs
{

}

[NetSerializable, Serializable]
public enum DiseaseMutationVisuals : byte
{
    state,
    infected
}


[Serializable, NetSerializable]
public sealed partial class CollectDiseaseDataDoAfterEvent : SimpleDoAfterEvent
{ }


public sealed partial class ShopMutationActionEvent : InstantActionEvent
{

}

public sealed partial class TeleportToPrimaryPatientEvent : InstantActionEvent
{

}
public sealed partial class SelectPrimaryPatientEvent : EntityTargetActionEvent
{

}
