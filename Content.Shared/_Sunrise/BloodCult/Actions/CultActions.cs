using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.Actions;

public sealed partial class CultTwistedConstructionActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultBloodMagicInstantActionEvent : InstantActionEvent
{
}

public sealed partial class CultSummonDaggerActionEvent : InstantActionEvent
{
}

public sealed partial class CultStunTargetActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultTeleportTargetActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultEmpPulseTargetActionEvent : InstantActionEvent
{
}

public sealed partial class CultShadowShacklesTargetActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultSummonCombatEquipmentTargetActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultConcealPresenceWorldActionEvent : WorldTargetActionEvent
{
}

public sealed partial class CultBloodRitualInstantActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ShadowShacklesDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class CultReturnBloodSpearActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CultMagicBloodCallEvent : SimpleDoAfterEvent
{
    public string? ActionId;
    public float BloodTake;
}

[Serializable, NetSerializable]
public sealed partial class CultConvertAirlockEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class TeleportSpellUsedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class TwistedConstructSpellUsedEvent : EntityEventArgs
{
}
