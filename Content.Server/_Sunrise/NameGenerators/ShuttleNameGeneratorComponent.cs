using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.NameGenerators;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleNameGeneratorComponent : Component
{
    [DataField]
    public string FactionIdentificator = "NT";

    [DataField]
    public bool EnableFragments = true;

    [DataField]
    public bool HasIdentificator;

    [DataField]
    public ProtoId<DatasetPrototype> NameFragments = "ru_shuttle_prefixes";
}
