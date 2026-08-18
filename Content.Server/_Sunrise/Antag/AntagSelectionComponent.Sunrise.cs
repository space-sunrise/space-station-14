using System.Runtime.InteropServices;
using Content.Server.Antag;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Antag.Components;

public sealed partial class AntagSelectionComponent
{
    /// <summary>
    /// Whether this rule created at least one ghost-role spawner.
    /// </summary>
    public bool UseSpawners;

    /// <summary>
    /// Number of ghost-role spawners created by this rule.
    /// </summary>
    public int SpawnersCount;
}

[StructLayout(LayoutKind.Auto)]
public partial struct AntagSelectionDefinition
{
    /// <summary>
    /// Maximum number of command staff that may be selected. Zero means unlimited.
    /// </summary>
    [DataField]
    public int MaxCommandStaff;

    /// <summary>
    /// Whether command staff may be selected by this definition.
    /// </summary>
    [DataField]
    public bool PickCommandStaff;

    /// <summary>
    /// Whether the normal job-based antagonist restriction may be ignored.
    /// </summary>
    [DataField]
    public bool IgnoreCanBeAntag;

    /// <summary>
    /// Whether antagonist preference checkboxes are ignored for this definition.
    /// </summary>
    [DataField]
    public bool IgnorePrefCheck;
}
