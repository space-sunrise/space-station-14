using System.Linq;
using System.Text.Json.Serialization;
using Content.Shared.FixedPoint;

namespace Content.Server._Sunrise.GuideGenerator;
public sealed class WikiReagentEffectsEntry
{
    [JsonPropertyName("rate")]
    public FixedPoint2 MetabolismRate { get; } = FixedPoint2.New(0.5f);

    [JsonPropertyName("effects")]
    public List<ReagentEffectEntry> Effects { get; } = new();

    public WikiReagentEffectsEntry(Shared.Chemistry.Reagent.ReagentEffectsEntry proto)
    {
        MetabolismRate = proto.MetabolismRate;
        Effects = proto.Effects.Select(x => new ReagentEffectEntry(x)).ToList();
    }

}
