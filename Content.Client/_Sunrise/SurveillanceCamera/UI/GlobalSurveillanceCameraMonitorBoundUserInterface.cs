using Content.Client.Eye;
using Content.Client.SurveillanceCamera;
using Content.Client.SurveillanceCamera.UI;
using Content.Shared._Sunrise.SurveillanceCamera;
using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.SurveillanceCamera.UI;

public sealed class GlobalSurveillanceCameraMonitorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SurveillanceCameraMonitorWindow? _window;

    [ViewVariables]
    private EntityUid? _currentCamera;

    private readonly IEntityManager _entManager;
    private readonly EyeLerpingSystem _eyeLerpingSystem;
    private readonly SurveillanceCameraMonitorSystem _surveillanceCameraMonitorSystem;

    public GlobalSurveillanceCameraMonitorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _entManager = IoCManager.Resolve<IEntityManager>();
        _eyeLerpingSystem = _entManager.EntitySysManager.GetEntitySystem<EyeLerpingSystem>();
        _surveillanceCameraMonitorSystem = _entManager.EntitySysManager.GetEntitySystem<SurveillanceCameraMonitorSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SurveillanceCameraMonitorWindow>();

        _window.CameraSelected += OnCameraSelected;
        _window.CameraRefresh += OnCameraRefresh;
        _window.SubnetRefresh += OnSubnetRefresh;
        _window.CameraSwitchTimer += OnCameraSwitchTimer;
        _window.CameraDisconnect += OnCameraDisconnect;

        _window.SetEntity(Owner);
    }

    private void OnCameraSelected(string cameraAddress, string subnetAddress)
    {
        SendMessage(new SurveillanceCameraMonitorSwitchMessage(cameraAddress, subnetAddress));
    }

    private void OnCameraSwitchTimer()
    {
        _surveillanceCameraMonitorSystem.AddTimer(Owner, _window!.OnSwitchTimerComplete);
    }

    private void OnCameraRefresh()
    {
        SendMessage(new SurveillanceCameraRefreshCamerasMessage());
    }

    private void OnSubnetRefresh()
    {
        SendMessage(new SurveillanceCameraRefreshSubnetsMessage());
    }

    private void OnCameraDisconnect()
    {
        SendMessage(new SurveillanceCameraDisconnectMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not GlobalSurveillanceCameraMonitorUiState cast)
            return;

        SetNavMapGrid(cast.BackgroundGrid);

        var active = EntMan.GetEntity(cast.ActiveCamera);
        _entManager.TryGetComponent<TransformComponent>(Owner, out var xform);

        if (active == null)
        {
            _window.UpdateState(null, cast.ActiveAddress, cast.ActiveCamera);
            ClearCurrentCamera();
        }
        else
        {
            if (_currentCamera == null)
            {
                _eyeLerpingSystem.AddEye(active.Value);
                _currentCamera = active;
            }
            else if (_currentCamera != active)
            {
                if (_entManager.EntityExists(_currentCamera.Value))
                    _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
                _eyeLerpingSystem.AddEye(active.Value);
                _currentCamera = active;
            }

            if (EntMan.TryGetComponent<EyeComponent>(active, out var eye))
            {
                _window.UpdateState(eye.Eye, cast.ActiveAddress, cast.ActiveCamera);
            }
        }

        _window.ShowCameras(cast.Cameras, xform?.Coordinates);
    }

    private void SetNavMapGrid(NetEntity? backgroundGrid)
    {
        if (backgroundGrid == null || _window == null)
            return;

        var gridUid = EntMan.GetEntity(backgroundGrid);
        if (gridUid == null)
            return;

        _window.SetMapGrid(gridUid.Value);
    }

    private void ClearCurrentCamera()
    {
        if (_currentCamera != null)
        {
            _surveillanceCameraMonitorSystem.RemoveTimer(Owner);
            if (_entManager.EntityExists(_currentCamera.Value))
                _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
            _currentCamera = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ClearCurrentCamera();

        if (disposing && _window != null)
        {
            _window.Orphan();
            _window = null;
        }
    }
}
