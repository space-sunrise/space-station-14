#nullable enable
// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130
using System.Collections.Generic;
using NFluidsynth;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;

namespace Content.IntegrationTests._Sunrise;

public sealed class DummyMidiManager : IMidiManager
{
    public IReadOnlyList<IMidiRenderer> Renderers => [];
    public bool IsAvailable => false;
    public float Gain { get; set; }

    public IMidiRenderer? GetNewRenderer(bool mono = true)
    {
        return null;
    }

    public RobustMidiEvent FromFluidEvent(MidiEvent midiEvent, uint tick)
    {
        throw new NotSupportedException();
    }

    public SequencerEvent ToSequencerEvent(RobustMidiEvent midiEvent)
    {
        throw new NotSupportedException();
    }

    public RobustMidiEvent FromSequencerEvent(SequencerEvent midiEvent, uint tick)
    {
        throw new NotSupportedException();
    }

    public void FrameUpdate(float frameTime)
    {
    }

    public void Shutdown()
    {
    }
}
