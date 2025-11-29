using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Timing;
using Content.Shared._Sunrise.AdvancedDevices;

namespace Content.Server._Sunrise.AdvancedDevices;

public sealed class MultiplexerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiplexerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MultiplexerComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<MultiplexerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MultiplexerComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MultiplexerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.StateA = component.StateA == SignalState.Momentary ? SignalState.Low : component.StateA;
            component.StateB = component.StateB == SignalState.Momentary ? SignalState.Low : component.StateB;
            component.StateC = component.StateC == SignalState.Momentary ? SignalState.Low : component.StateC;
            component.StateD = component.StateD == SignalState.Momentary ? SignalState.Low : component.StateD;
            component.SelectA = component.SelectA == SignalState.Momentary ? SignalState.Low : component.SelectA;
            component.SelectB = component.SelectB == SignalState.Momentary ? SignalState.Low : component.SelectB;
            component.DemuxInputState = component.DemuxInputState == SignalState.Momentary ? SignalState.Low : component.DemuxInputState;

            UpdateOutputs(uid, component);
        }
    }

    private void OnInit(EntityUid uid, MultiplexerComponent comp, ComponentInit args)
    {
        SwitchPorts(uid, comp);
    }

    private void SwitchPorts(EntityUid uid, MultiplexerComponent comp)
    {
        _entityManager.RemoveComponent<DeviceLinkSinkComponent>(uid);
        _entityManager.RemoveComponent<DeviceLinkSourceComponent>(uid);

        if (comp.State == MuxState.Mux)
        {
            _deviceLink.EnsureSinkPorts(uid, comp.InputPortA, comp.InputPortB, comp.InputPortC, comp.InputPortD, comp.SelectPortA, comp.SelectPortB);
            _deviceLink.EnsureSourcePorts(uid, comp.OutputMuxPort);
        }
        else
        {
            _deviceLink.EnsureSinkPorts(uid, comp.InputDemuxPort, comp.SelectPortA, comp.SelectPortB);
            _deviceLink.EnsureSourcePorts(uid, comp.OutputPortA, comp.OutputPortB, comp.OutputPortC, comp.OutputPortD);
        }
    }

    private void OnExamined(EntityUid uid, MultiplexerComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var stateText = comp.State == MuxState.Mux ? "multiplexer" : "demultiplexer";
        args.PushMarkup(Loc.GetString("multiplexer-state", ("state", stateText)));
    }

    private void OnInteractUsing(EntityUid uid, MultiplexerComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, comp.CycleQuality))
            return;

        if (TryComp<UseDelayComponent>(uid, out var useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        comp.State = comp.State == MuxState.Mux ? MuxState.Demux : MuxState.Mux;

        SwitchPorts(uid, comp);
        UpdateOutputs(uid, comp);

        _audio.PlayPvs(comp.CycleSound, uid);
        var msg = Loc.GetString("multiplexer-mode-switch", ("mode", comp.State == MuxState.Mux ? "MUX" : "DEMUX"));
        _popup.PopupEntity(msg, uid, args.User);
        _appearance.SetData(uid, MultiplexerVisuals.Gate, comp.State);

        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, MultiplexerComponent comp, ref SignalReceivedEvent args)
    {
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        switch (args.Port)
        {
            case var _ when args.Port == comp.InputPortA: comp.StateA = state; break;
            case var _ when args.Port == comp.InputPortB: comp.StateB = state; break;
            case var _ when args.Port == comp.InputPortC: comp.StateC = state; break;
            case var _ when args.Port == comp.InputPortD: comp.StateD = state; break;
            case var _ when args.Port == comp.SelectPortA: comp.SelectA = state; break;
            case var _ when args.Port == comp.SelectPortB: comp.SelectB = state; break;
            case var _ when args.Port == comp.InputDemuxPort: comp.DemuxInputState = state; break;
        }

        UpdateOutputs(uid, comp);
    }

    private void UpdateOutputs(EntityUid uid, MultiplexerComponent comp)
    {
        if (comp.State == MuxState.Mux)
        {
            UpdateMuxOutput(uid, comp);
        }
        else
        {
            UpdateDemuxOutputs(uid, comp);
        }
    }

    private void UpdateMuxOutput(EntityUid uid, MultiplexerComponent comp)
    {
        var a = comp.StateA != SignalState.Low;
        var b = comp.StateB != SignalState.Low;
        var c = comp.StateC != SignalState.Low;
        var d = comp.StateD != SignalState.Low;
        var selA = comp.SelectA != SignalState.Low;
        var selB = comp.SelectB != SignalState.Low;

        var output = selB ? (selA ? d : c) : (selA ? b : a);

        if (output != comp.LastMuxOutput)
        {
            comp.LastMuxOutput = output;
            _deviceLink.SendSignal(uid, comp.OutputMuxPort, output);
        }
    }

    private void UpdateDemuxOutputs(EntityUid uid, MultiplexerComponent comp)
    {
        var input = comp.DemuxInputState != SignalState.Low;
        var selA = comp.SelectA != SignalState.Low;
        var selB = comp.SelectB != SignalState.Low;

        var outputA = false;
        var outputB = false;
        var outputC = false;
        var outputD = false;

        if (input)
        {
            if (!selB && !selA) outputA = true;
            else if (!selB && selA) outputB = true;
            else if (selB && !selA) outputC = true;
            else if (selB && selA) outputD = true;
        }

        if (outputA != comp.LastDemuxOutputA)
        {
            comp.LastDemuxOutputA = outputA;
            _deviceLink.SendSignal(uid, comp.OutputPortA, outputA);
        }

        if (outputB != comp.LastDemuxOutputB)
        {
            comp.LastDemuxOutputB = outputB;
            _deviceLink.SendSignal(uid, comp.OutputPortB, outputB);
        }

        if (outputC != comp.LastDemuxOutputC)
        {
            comp.LastDemuxOutputC = outputC;
            _deviceLink.SendSignal(uid, comp.OutputPortC, outputC);
        }

        if (outputD != comp.LastDemuxOutputD)
        {
            comp.LastDemuxOutputD = outputD;
            _deviceLink.SendSignal(uid, comp.OutputPortD, outputD);
        }
    }
}