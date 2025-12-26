using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.FleshCult;

[RegisterComponent]
public sealed partial class PendingFleshCultistComponent : Component
{
    [DataField("firstStageTimer")]
    public float FirstStageTimer = 30;

    [DataField("secondStageTimer")]
    public float SecondStageTimer = 30;

    [DataField("currentStage")]
    public PendingFleshCultistStage CurrentStage;

    [DataField("nextParalyze", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextParalyze;

    [DataField("paralyzeInterval")]
    public float ParalyzeInterval = 10;

    [DataField("paralyzeTime")]
    public float ParalyzeTime = 5;

    [DataField("nextScream", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextScream;

    [DataField("screamInterval")]
    public float ScreamInterval = 5;

    [DataField("nextStutter", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextStutter;

    [DataField("stutterTime")]
    public float StutterTime = 10;

    [DataField("stutterInterval")]
    public float StutterInterval = 3;

    [DataField("nextJitter", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextJitter;

    [DataField("jitterTime")]
    public float JitterTime = 10;

    [DataField("jitterInterval")]
    public float JitterInterval = 5;

    public float Accumulator = 0;
}

public enum PendingFleshCultistStage
{
    First,
    Second,
    Third,
    Final,
}
