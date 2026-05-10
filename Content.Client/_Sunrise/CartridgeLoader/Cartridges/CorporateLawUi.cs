using Content.Client.UserInterface.Fragments;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.CartridgeLoader.Cartridges;

public sealed partial class CorporateLawUi : UIFragment
{
    private CorporateLawUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CorporateLawUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is CorporateLawUiState castState)
            _fragment?.UpdateState(castState);
    }
}
