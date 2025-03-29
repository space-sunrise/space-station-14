namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneBaseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cultistGatheringRange")]
    public float CultistGatheringRange = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("gatherInvokers")]
    public bool GatherInvokers = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("invokePhrase")]
    public string InvokePhrase = "";

    [ViewVariables(VVAccess.ReadWrite), DataField("invokersMinCount")]
    public uint InvokersMinCount = 1;
}
