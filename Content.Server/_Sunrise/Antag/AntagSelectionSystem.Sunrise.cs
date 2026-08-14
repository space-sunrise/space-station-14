using Content.Server.Antag.Components;
using Content.Shared.Tag;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    private void ApplySunriseAntagTags(EntityUid player, AntagSelectionDefinition def)
    {
        _tag.AddTags(player, def.Tags);
    }
}
