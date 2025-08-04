// Компонент вешается на цель (гуманоида), чтобы пометить, что он "ожидает превращения в аномалию" (таймер ещё идёт)
using Robust.Shared.GameObjects;

namespace Content.Shared.Anomaly.Components
{
    [RegisterComponent]
    public sealed partial class PendingAnomalyInfectionComponent : Component
    {
    }
}
