﻿using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared._Sunrise.Medical.Surgery;

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public readonly EntProtoId Surgery;
    public readonly EntProtoId Step;

    public SurgeryDoAfterEvent(EntProtoId surgery, EntProtoId step)
    {
        Surgery = surgery;
        Step = step;
    }
}
