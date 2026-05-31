using Content.Shared._Sunrise.Disease;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Disease.UI;

[UsedImplicitly]
public sealed class DiseaseInfoBui : BoundUserInterface
{
    [ViewVariables]
    private DiseaseInfoWindow? _window;

    public DiseaseInfoBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new DiseaseInfoWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DiseaseInfoState diseaseState)
            return;

        _window?.UpdateState(
            diseaseState.BaseInfectChance,
            diseaseState.CoughSneezeInfectChance,
            diseaseState.Lethal,
            diseaseState.Shield,
            diseaseState.CurrentInfected,
            diseaseState.TotalInfected
        );
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
