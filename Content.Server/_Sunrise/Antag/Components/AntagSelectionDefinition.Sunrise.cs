using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

public partial struct AntagSelectionDefinition
{
    /// <summary>
    /// Tags added to the player when this antag definition is applied.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<TagPrototype>> Tags = new();
}
