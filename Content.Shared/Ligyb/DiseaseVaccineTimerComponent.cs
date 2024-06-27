namespace Content.Shared.Ligyb;

[RegisterComponent]
public sealed partial class DiseaseVaccineTimerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ReadyAt = TimeSpan.Zero;

    [DataField] public TimeSpan Delay = TimeSpan.FromMinutes(5);

    [DataField] public float SpeedBefore = 0;
    [DataField] public bool Immune;
}
