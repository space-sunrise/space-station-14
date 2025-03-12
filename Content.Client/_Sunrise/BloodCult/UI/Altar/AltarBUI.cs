using Content.Shared._Sunrise.BloodCult.UI;
using JetBrains.Annotations;

namespace Content.Client._Sunrise.BloodCult.UI.Altar;

[UsedImplicitly]
public sealed class AltarBUI : BoundUserInterface
{
    private AltarWindow? _window;

    public AltarBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new AltarWindow();

        _window.OnClose += Close;
        _window.OnItemSelected += OnItemSelected;
    }

    private void OnItemSelected(string item)
    {
        var evt = new AltarBuyRequest(item);
        SendMessage(evt);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;
        _window?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AltarListingBUIState listingState)
        {
            _window?.SetListing(listingState.Items);
        }
        else if (state is AltarTimerBUIState timerState)
        {
            _window?.SetTimer(timerState.NextTimeUse);
        }
    }
}
