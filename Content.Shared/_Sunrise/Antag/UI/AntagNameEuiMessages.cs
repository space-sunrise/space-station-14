using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Antag.UI;

[Serializable, NetSerializable]
public sealed class AntagNameEuiState(
    string currentName,
    string roleTitle,
    int maxNameLength)
    : EuiStateBase
{
    public string CurrentName { get; } = currentName;
    public string RoleTitle { get; } = roleTitle;
    public int MaxNameLength { get; } = maxNameLength;
}

[Serializable, NetSerializable]
public sealed class AntagNameSelectedMessage(string? name, bool keepRandom)
    : EuiMessageBase
{
    public string? Name { get; } = name;
    public bool KeepRandom { get; } = keepRandom;
}
