using Content.Shared._Sunrise.CriminalRecords;
using Robust.Shared.Serialization;

namespace Content.Server._Sunrise.CriminalRecords.Components;

[RegisterComponent]
public sealed partial class PrisonerManagementConsoleComponent : Component
{
    /// <summary>
    ///     Active incarcerations mapped by cell index (0-9).
    /// </summary>
    [ViewVariables]
    public Dictionary<int, ActiveIncarceration> ActiveIncarcerations = new();
}
