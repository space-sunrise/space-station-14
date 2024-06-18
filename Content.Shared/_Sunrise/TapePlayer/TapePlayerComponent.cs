using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TapePlayer;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTapePlayerSystem))]
public sealed partial class TapePlayerComponent : Component
{
    public const string TapeSlotId = "tape";

    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    [DataField, AutoNetworkedField]
    public EntityUid? InsertedTape;

    [DataField(required: true)]
    public ItemSlot TapeSlot = new();

    [DataField, AutoNetworkedField]
    [ViewVariables]
    public float Volume = 0.5f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RolloffFactor = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxDistance = 20f;

    [DataField]
    public string? OnState;

    [DataField]
    public string? OffState;

    [DataField]
    public bool NeedPower;

    [DataField]
    public bool Loop = true;

    [DataField]
    public SoundSpecifier? ButtonSound;
}

[Serializable, NetSerializable]
public sealed class TapePlayerPlayingMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TapePlayerPauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TapePlayerStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TapePlayerSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}
[Serializable, NetSerializable]
public sealed class TapePlayerSetVolumeMessage(float volume) : BoundUserInterfaceMessage
{
    public float Volume { get; } = volume;
}

[Serializable, NetSerializable]
public enum TapePlayerVisuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum TapePlayerVisualState : byte
{
    On,
    Off,
}

public enum TapePlayerVisualLayers : byte
{
    Base,
}
