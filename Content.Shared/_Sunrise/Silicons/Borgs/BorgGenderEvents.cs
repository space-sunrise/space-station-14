using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Silicons.Borgs;

/// <summary>
/// Action event used to open the borg gender menu.
/// </summary>
public sealed partial class BorgGenderChangeActionEvent : InstantActionEvent;

/// <summary>
/// BUI message used to change the selected borg gender.
/// </summary>
[Serializable, NetSerializable]
public sealed class BorgGenderChangeMessage(BorgGender gender) : BoundUserInterfaceMessage
{
    public BorgGender Gender = gender;
}

/// <summary>
/// BUI state for the borg gender menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class BorgGenderBuiState(BorgGender selectedGender) : BoundUserInterfaceState
{
    public BorgGender SelectedGender = selectedGender;
}

/// <summary>
/// Raised after a borg body's selected gender changes.
/// </summary>
[ByRefEvent]
public readonly record struct BorgGenderChangedEvent(BorgGender OldGender, BorgGender NewGender);
