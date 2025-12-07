using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// Request event to get the list of online administrators
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestAdminWhoEvent : EntityEventArgs
{
}

/// <summary>
/// Response event containing the list of online administrators
/// </summary>
[Serializable, NetSerializable] 
public sealed class AdminWhoResponseEvent : EntityEventArgs
{
    public readonly List<AdminWhoEntry> Admins;

    public AdminWhoResponseEvent(List<AdminWhoEntry> admins)
    {
        Admins = admins;
    }
}

/// <summary>
/// Information about a single administrator
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminWhoEntry
{
    public readonly string Name;
    public readonly string? Title;
    public readonly bool IsStealth;
    public readonly bool IsAfk;

    public AdminWhoEntry(string name, string? title, bool isStealth, bool isAfk)
    {
        Name = name;
        Title = title;
        IsStealth = isStealth;
        IsAfk = isAfk;
    }
}