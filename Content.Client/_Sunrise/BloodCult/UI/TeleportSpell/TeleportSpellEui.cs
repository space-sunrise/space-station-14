using System.Linq;
using Content.Client._Sunrise.BloodCult.UI.TeleportRunesList;
using Content.Client.Eui;
using Content.Shared._Sunrise.BloodCult.UI;
using Content.Shared.Eui;

namespace Content.Client._Sunrise.BloodCult.UI.TeleportSpell;

public sealed class TeleportSpellEui : BaseEui
{
    private TeleportRunesListWindow _window;

    public TeleportSpellEui()
    {
        _window = new TeleportRunesListWindow();
    }

    public override void Opened()
    {
        _window.OpenCentered();
        _window.ItemSelected += (index, _) => SendMessage(new TeleportSpellTargetRuneSelected() { RuneUid = index });
        _window.OnClose += () => SendMessage(new CloseEuiMessage());

        base.Opened();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not TeleportSpellEuiState cast)
            return;

        _window.Clear();
        _window.PopulateList(cast.Runes.Keys.ToList(), cast.Runes.Values.ToList());
    }
}
