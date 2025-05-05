using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Реакция для синтеза Нитриума из трития, азота и BZ. by TERRISS
/// </summary>
[UsedImplicitly]
public sealed partial class NitriumSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initTritium = mixture.GetMoles(Gas.Tritium);
        var initNitrogen = mixture.GetMoles(Gas.Nitrogen);
        var initBZ = mixture.GetMoles(Gas.BZ);
        var temperature = mixture.Temperature;

        // Проверка условий для реакции
        if (initTritium < 2 || initNitrogen < 2 || initBZ < 1 || temperature < 1500 || temperature > 13000)
            return ReactionResult.NoReaction;

        // Логика реакции
        var tritiumRemoved = 5f; // Количество удаляемого трития
        var nitrogenRemoved = 5f; // Количество удаляемого азота
        var bzRemoved = 1f; // Количество удаляемого BZ
        var nitriumProduced = 12f; // Количество производимого Нитриума

        if (tritiumRemoved > initTritium || nitrogenRemoved > initNitrogen || bzRemoved > initBZ)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Tritium, -tritiumRemoved);
        mixture.AdjustMoles(Gas.Nitrogen, -nitrogenRemoved);
        mixture.AdjustMoles(Gas.BZ, -bzRemoved);
        mixture.AdjustMoles(Gas.Nitrium, nitriumProduced);

        return ReactionResult.Reacting;
    }
}
