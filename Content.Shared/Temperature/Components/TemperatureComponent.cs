using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Handles changing temperature,
/// informing others of the current temperature.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TemperatureComponent : Component
{
    /// <summary>
    /// Surface temperature which is modified by the environment.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentTemperature = Atmospherics.T20C;

    /// <summary>
    /// Heat capacity per kg of mass.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpecificHeat = 50f;

    /// <summary>
    /// How well does the air surrounding you merge into your body temperature?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AtmosTemperatureTransferEfficiency = 0.1f;

    /// <summary>
    ///     Should this entity change its color based on temperature?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool ColorTemperature = false;

    /// <summary>
    ///     Allow coloring when cold?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool ColorCold = true;

    /// <summary>
    ///     Allow coloring when hot?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool ColorHot = true;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color ColdColor = Color.FromHex("#70b0ff");

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color HotColor = Color.FromHex("#ff4000");

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float NeutralTemp = 293.15f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ColdThreshold = 250f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float HotThreshold = 450f;
}
