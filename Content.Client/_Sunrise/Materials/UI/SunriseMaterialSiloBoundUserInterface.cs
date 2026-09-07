using Content.Shared._Sunrise.Materials.MaterialSilo;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Materials.UI;

[UsedImplicitly]
public sealed class SunriseMaterialSiloBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SunriseMaterialSiloMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SunriseMaterialSiloMenu>();
        _menu.SetEntity(Owner);

        _menu.OnClientEntryPressed += netEnt =>
        {
            SendPredictedMessage(new ToggleSunriseMaterialSiloClientMessage(netEnt));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SunriseMaterialSiloBuiState msg)
            return;
        _menu?.Update(msg);
    }
}
