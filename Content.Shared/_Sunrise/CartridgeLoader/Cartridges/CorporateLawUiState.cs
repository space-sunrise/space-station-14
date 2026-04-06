using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CorporateLawUiState : BoundUserInterfaceState
{
    public readonly List<LawSection> Sections;

    public CorporateLawUiState(List<LawSection> sections)
    {
        Sections = sections;
    }
}

[Serializable, NetSerializable]
public struct LawSection
{
    public string Title;
    public Color? Color;
    public List<LawEntry> Entries;
}

[Serializable, NetSerializable]
public struct LawEntry
{
    public string? Identifier;
    public string Title;
    public string Description;
}
