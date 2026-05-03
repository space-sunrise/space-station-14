using Content.Server.Instruments;
using NUnit.Framework;
using Robust.Shared.Audio.Midi;

namespace Content.Tests.Server._Sunrise.Instruments;

[TestFixture]
public sealed class InstrumentMidiValidationTests
{
    [Test]
    public void IsValidChannel_RejectsOutOfRangeChannels()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InstrumentMidiValidation.IsValidChannel(-1), Is.False);
            Assert.That(InstrumentMidiValidation.IsValidChannel(0), Is.True);
            Assert.That(InstrumentMidiValidation.IsValidChannel(RobustMidiEvent.MaxChannels - 1), Is.True);
            Assert.That(InstrumentMidiValidation.IsValidChannel(RobustMidiEvent.MaxChannels), Is.False);
        });
    }

    [Test]
    public void IsValidBatch_AcceptsSupportedSafeEvents()
    {
        var batch = new[]
        {
            RobustMidiEvent.NoteOn(0, 60, 100, 1),
            RobustMidiEvent.NoteOff(0, 60, 2),
            RobustMidiEvent.AfterTouch(0, 60, 100, 3),
            RobustMidiEvent.ControlChange(0, 1, 64, 4),
            RobustMidiEvent.ProgramChange(0, 32, 5),
            RobustMidiEvent.ChannelPressure(0, 64, 6),
            new RobustMidiEvent(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.PitchBend), 0x7F, 0x7F, 7),
            RobustMidiEvent.AllNotesOff(15, 8),
            RobustMidiEvent.SystemReset(9),
        };

        Assert.That(InstrumentMidiValidation.IsValidBatch(batch), Is.True);
    }

    [Test]
    public void IsValidBatch_RejectsEmptyBatch()
    {
        Assert.That(InstrumentMidiValidation.IsValidBatch([]), Is.False);
    }

    [TestCaseSource(nameof(InvalidMidiEvents))]
    public void IsValidEvent_RejectsMalformedPayload(RobustMidiEvent midiEvent)
    {
        Assert.That(InstrumentMidiValidation.IsValidEvent(midiEvent), Is.False);
    }

    private static readonly RobustMidiEvent[] InvalidMidiEvents =
    {
        new(0x70, 0, 0, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.NoteOn), 0x80, 0x40, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.NoteOn), 0x40, 0x80, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.ControlChange), 0x80, 0x40, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.ControlChange), 0x40, 0x80, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.ProgramChange), 0x80, 0x00, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.ChannelPressure), 0x80, 0x00, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.PitchBend), 0x80, 0x40, 0),
        new(RobustMidiEvent.MakeStatus(0, RobustMidiCommand.SystemMessage), 0x01, 0x00, 0),
        new(0xFF, 0x00, 0x01, 0),
    };
}
