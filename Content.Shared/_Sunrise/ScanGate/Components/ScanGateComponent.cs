using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.ScanGate.Components;

/// <summary>
/// Marks an entity as a scan gate that can detect entities with <see cref="ScanDetectableComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ScanGateComponent : Component
{
    /// <summary>
    /// The delay between scans.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The next time the scan gate can perform a scan.
    /// </summary>
    [AutoNetworkedField, AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextScanTime = TimeSpan.Zero;

    /// <summary>
    /// The time when the scan gate visual state should return to idle.
    /// </summary>
    [AutoNetworkedField, AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StateResetTime = TimeSpan.Zero;

    /// <summary>
    /// The sound played when a scan is performed.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanSound = new SoundCollectionSpecifier("ScanGateScan");

    /// <summary>
    /// The sound played when a scan successfully detects an item.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanFailSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/ScanGate/scan_fail.ogg");

    /// <summary>
    /// Sprite state to set on successful scan.
    /// </summary>
    [DataField]
    public string ScanSuccessState = "success";

    /// <summary>
    /// Sprite state to set on failed scan.
    /// </summary>
    [DataField]
    public string ScanFailState = "fail";

    /// <summary>
    /// Sprite state to set when idle.
    /// </summary>
    [DataField]
    public string IdleState = "idle";

    /// <summary>
    /// The signal to send on successful scan.
    /// </summary>
    [DataField]
    public string SuccessSignal = "ScanGateSuccess";

    /// <summary>
    /// The signal to send on failed scan.
    /// </summary>
    [DataField]
    public string FailSignal = "ScanGateFail";
}
