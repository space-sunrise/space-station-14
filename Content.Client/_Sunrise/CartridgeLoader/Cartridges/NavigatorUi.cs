using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class NavigatorUi : UIFragment
{
    private NavigatorUiFragment? _fragment;
    private IEntityManager? _entManager;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _entManager = IoCManager.Resolve<IEntityManager>();
        _fragment = new NavigatorUiFragment();
        _fragment.Setup(fragmentOwner);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NavigatorUiState navigatorState || _entManager == null)
            return;

        var mapUid = _entManager.GetEntity(navigatorState.MapUid);
        _fragment?.UpdateState(mapUid, navigatorState.StationName);
    }
}
