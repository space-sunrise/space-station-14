using Robust.Shared.Prototypes;

namespace Content.Shared.Voting;

[Prototype("voteRandom")]
public sealed partial class VoteRandomPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("chances")]
    public Dictionary<string, float> Chances { get; private set; } = new();

    private VoteRandomPrototype() { }

    public VoteRandomPrototype(string id, Dictionary<string, float> chances)
    {
        ID = id;
        Chances = chances;
    }
}
