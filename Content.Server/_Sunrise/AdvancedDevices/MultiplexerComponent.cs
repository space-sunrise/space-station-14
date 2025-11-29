using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Tools;
using Content.Shared._Sunrise.AdvancedDevices;

namespace Content.Server._Sunrise.AdvancedDevices;

[RegisterComponent, Access(typeof(MultiplexerSystem))]
public sealed partial class MultiplexerComponent : Component
{
    [DataField] public MuxState State = MuxState.Mux;
    [DataField] public SoundSpecifier CycleSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
    [DataField] public ProtoId<ToolQualityPrototype> CycleQuality = "Screwing";

    [DataField] public ProtoId<SinkPortPrototype> InputPortA = "MuxInputA";
    [DataField] public ProtoId<SinkPortPrototype> InputPortB = "MuxInputB";
    [DataField] public ProtoId<SinkPortPrototype> InputPortC = "MuxInputC";
    [DataField] public ProtoId<SinkPortPrototype> InputPortD = "MuxInputD";

    [DataField] public ProtoId<SourcePortPrototype> OutputPortA = "MuxOutputA";
    [DataField] public ProtoId<SourcePortPrototype> OutputPortB = "MuxOutputB";
    [DataField] public ProtoId<SourcePortPrototype> OutputPortC = "MuxOutputC";
    [DataField] public ProtoId<SourcePortPrototype> OutputPortD = "MuxOutputD";

    [DataField] public ProtoId<SinkPortPrototype> SelectPortA = "MuxSelectA";
    [DataField] public ProtoId<SinkPortPrototype> SelectPortB = "MuxSelectB";

    [DataField] public ProtoId<SourcePortPrototype> OutputMuxPort = "MuxOutput";
    [DataField] public ProtoId<SinkPortPrototype> InputDemuxPort = "DemuxInput";

    [DataField] public SignalState StateA = SignalState.Low;
    [DataField] public SignalState StateB = SignalState.Low;
    [DataField] public SignalState StateC = SignalState.Low;
    [DataField] public SignalState StateD = SignalState.Low;
    [DataField] public SignalState SelectA = SignalState.Low;
    [DataField] public SignalState SelectB = SignalState.Low;
    [DataField] public SignalState DemuxInputState = SignalState.Low;

    [DataField] public bool LastMuxOutput;
    [DataField] public bool LastDemuxOutputA;
    [DataField] public bool LastDemuxOutputB;
    [DataField] public bool LastDemuxOutputC;
    [DataField] public bool LastDemuxOutputD;
    [DataField] public string? Error = null;
}