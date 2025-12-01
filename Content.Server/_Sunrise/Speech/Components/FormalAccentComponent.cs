using Content.Server._Sunrise.Speech.EntitySystems;
using Content.Server.Speech.EntitySystems;

namespace Content.Server._Sunrise.Speech.Components;

/// <summary>
/// Formal accent expands abbreviations to their full formal meanings.
/// </summary>
[RegisterComponent]
[Access(typeof(FormalAccentSystem))]
public sealed partial class FormalAccentComponent : Component {} // Fish-edit
