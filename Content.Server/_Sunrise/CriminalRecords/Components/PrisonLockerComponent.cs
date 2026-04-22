namespace Content.Server._Sunrise.CriminalRecords.Components;

[RegisterComponent]
public sealed partial class PrisonLockerComponent : Component
{
    /// <summary>
    ///     The specific access ID required to open this locker.
    ///     This ID will be used to identify and consume the prisoner's ID card.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string? AccessId;

    /// <summary>
    ///     The last user who attempted to open this locker.
    /// </summary>
    [ViewVariables]
    public EntityUid? LastUser;
}
