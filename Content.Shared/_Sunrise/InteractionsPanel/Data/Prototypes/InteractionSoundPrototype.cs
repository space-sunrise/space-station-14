using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;

[Prototype]
public sealed partial class InteractionSoundPrototype: IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public SoundSpecifier Sound { get; private set; } = default!;
}
