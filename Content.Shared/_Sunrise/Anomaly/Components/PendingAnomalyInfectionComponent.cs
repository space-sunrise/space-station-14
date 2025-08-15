using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System;

/// <summary>
/// Компонент вешается на цель (гуманоида), чтобы пометить, что он "ожидает превращения в аномалию" (таймер ещё идёт)
/// </summary>

namespace Content.Shared._Sunrise.Anomaly.Components
{
    [RegisterComponent]
    public sealed partial class PendingAnomalyInfectionComponent : Component
    {
        public TimeSpan EndAt;
        public int CellularDamage;
        public EntProtoId? SelectedAnomalyTrapProtoId;
    }
}
