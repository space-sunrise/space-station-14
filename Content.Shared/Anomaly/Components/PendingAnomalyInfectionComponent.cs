using Robust.Shared.GameObjects;

namespace Content.Shared.Anomaly.Components
{
    // ВАЖНО: PendingAnomalyInfectionComponent используется как маркер "цель ожидает заражения аномалией" и вешается на моба.
    // AnomalyAutoInjectorComponent используется только на предмете-инъекторе. Объединять их нельзя, иначе логика предмета и цели смешается.
    [RegisterComponent]
    public sealed partial class PendingAnomalyInfectionComponent : Component
    {
    }
}
