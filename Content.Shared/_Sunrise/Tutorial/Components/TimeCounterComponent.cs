using System;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._Sunrise.Tutorial.Components;

/// <summary>
/// Networked configuration and state for the tutorial time counter UI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TimeCounterComponent : Component
{
    /// <summary>
    /// Absolute game time when the counter reaches zero.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? EndTime;

    /// <summary>
    /// Optional screen-space position override for the counter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2? ScreenPosition;

    /// <summary>
    /// Font size used by the counter text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FontSize = 30;

    /// <summary>
    /// Whether the counter should be centered around <see cref="ScreenPosition"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Centered = true;

    /// <summary>
    /// Counter text color before warning thresholds are reached.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color DefaultColor = Color.White;

    /// <summary>
    /// Counter text color used when remaining time is below <see cref="WarningTime"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color WarningColor = Color.Yellow;

    /// <summary>
    /// Counter text color used when remaining time is below <see cref="CriticalTime"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color CriticalColor = Color.Red;

    /// <summary>
    /// Background color behind the counter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color BackgroundColor = Color.Transparent;

    /// <summary>
    /// Border color around the counter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color BorderColor = Color.Transparent;

    /// <summary>
    /// Remaining time threshold for switching to <see cref="WarningColor"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan WarningTime = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Remaining time threshold for switching to <see cref="CriticalColor"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CriticalTime = TimeSpan.FromSeconds(30);
}
