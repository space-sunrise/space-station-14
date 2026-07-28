using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared._Sunrise.Antags.Vampires;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Antags.Vampires.Events;

public sealed partial class VampireGlareActionEvent : InstantActionEvent
{
    [DataField]
    public float Range = 1f;

    [DataField]
    public float FlashImmunityEffectScaleWeak;

    [DataField]
    public float FlashImmunityEffectScaleMid = 0.75f;

    [DataField]
    public float FlashImmunityEffectScaleStrong = 1f;

    [DataField]
    public float GlareEffectScaleFull = 1.5f;

    [DataField]
    public TimeSpan FrontParalyzeDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan SideParalyzeDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public float FrontStaminaDamage = 25f;

    [DataField]
    public float BehindStaminaDamage = 25f;

    [DataField]
    public float SideStaminaDamage = 25f;

    [DataField]
    public Dictionary<string, FixedPoint2> Reagents = new()
    {
        { "MuteToxin", 0.5 },
    };

    [DataField]
    public float ForwardDotThreshold = 0.7f;

    [DataField]
    public float BackwardDotThreshold = -0.7f;
}

public sealed partial class VampireSleepActionEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan ChannelTime = TimeSpan.FromSeconds(5);

    [DataField]
    public float SleepDistanceThreshold = 2.5f;

    [DataField]
    public float SleepMovementThreshold = 0.1f;
}

[Serializable, NetSerializable]
public sealed partial class VampireSleepDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public NetEntity Victim;

    [DataField]
    public float MaxDistance = 2.5f;

    [DataField]
    public int BloodCost = 15;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);
}

[Serializable, NetSerializable]
public sealed partial class VampireDrinkBloodDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class VampireDevourDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public float BloodFullnessRestore;
}

public sealed partial class VampireRejuvenateIActionEvent : InstantActionEvent
{
    [DataField]
    public bool ResetStamina = true;

    [DataField]
    public bool RemoveStuns = true;
}

public sealed partial class VampireRejuvenateIIActionEvent : InstantActionEvent
{
    [DataField]
    public bool ResetStamina = true;

    [DataField]
    public bool RemoveStuns = true;

    [DataField]
    public FixedPoint2 ReagentPurgeAmount = FixedPoint2.New(10);

    [DataField]
    public HashSet<string> PurgedMetabolismGroups = new()
    {
        "Poison",
    };

    [DataField]
    public int HealTicks = 5;

    [DataField]
    public TimeSpan HealTickInterval = TimeSpan.FromSeconds(3.5);

    [DataField]
    public Dictionary<string, FixedPoint2> HealGroups = new()
    {
        { "Brute", FixedPoint2.New(4) },
        { "Burn", FixedPoint2.New(4) },
    };

    [DataField]
    public Dictionary<string, FixedPoint2> HealTypes = new()
    {
        { "Poison", FixedPoint2.New(4) },
        { "Asphyxiation", FixedPoint2.New(5) },
    };
}

public sealed partial class VampireToggleFangsActionEvent : InstantActionEvent;
