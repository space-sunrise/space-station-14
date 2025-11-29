using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.AdvancedDevices;

[Serializable, NetSerializable]
public enum MultiplexerVisuals : byte
{
    Gate
}

[Serializable, NetSerializable]
public enum MultiplexerLayers : byte
{
    Gate
}

[Serializable, NetSerializable]
public enum MuxState : byte { Demux, Mux }