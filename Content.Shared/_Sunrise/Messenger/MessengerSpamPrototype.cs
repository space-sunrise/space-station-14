using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Messenger;

[Prototype]
public sealed partial class MessengerSpamPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string SenderDataset { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string MessageDataset { get; private set; } = string.Empty;

    [DataField]
    public string? NameDataset { get; private set; }

    [DataField]
    public string? SurnameDataset { get; private set; }
}
