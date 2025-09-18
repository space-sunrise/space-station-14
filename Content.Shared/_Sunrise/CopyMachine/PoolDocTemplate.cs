using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CopyMachine;

[Prototype]
public sealed partial class DocTemplatePoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<DocTemplatePrototype>> Templates = new ();
}
