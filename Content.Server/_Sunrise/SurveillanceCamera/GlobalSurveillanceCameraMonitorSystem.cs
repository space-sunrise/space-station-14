using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.SurveillanceCamera;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.SurveillanceCamera;
using Robust.Shared.Map;
using Content.Server._Sunrise.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Content.Shared.UserInterface;
using Content.Shared.Power;
using Robust.Shared.GameObjects;
using Content.Shared._Sunrise.SurveillanceCamera;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.SurveillanceCamera;

public sealed class GlobalSurveillanceCameraMonitorSystem : EntitySystem
{
    [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private readonly Dictionary<EntityUid, GlobalCameraState> _states = new();

    private const float UpdateInterval = 3f;
    private const int MaxViewersPerMonitor = 10;

    private static readonly Color[] MapColors = new[]
    {
        Color.Red, Color.Blue, Color.Green, Color.Orange,
        Color.Cyan, Color.Magenta, Color.Yellow, Color.White,
        Color.Pink, Color.Lime, Color.Aqua, Color.Gold,
        Color.Purple, Color.Teal, Color.Coral, Color.Salmon
    };

    private sealed class GlobalCameraState
    {
        public EntityUid? ActiveCamera;
        public string ActiveCameraAddress = string.Empty;
        public readonly HashSet<EntityUid> Viewers = new();
        public float UpdateTimer;
        public EntityUid? ActiveGridUid;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, BoundUIClosedEvent>(OnUIClose);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, SurveillanceCameraRefreshCamerasMessage>(OnRefresh);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, SurveillanceCameraRefreshSubnetsMessage>(OnRefresh);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, SurveillanceCameraMonitorSwitchMessage>(OnSwitch);
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, SurveillanceCameraDisconnectMessage>(OnDisconnect);

        // Entity lifecycle
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, EntityTerminatingEvent>(OnMonitorTerminating);
        SubscribeLocalEvent<SurveillanceCameraComponent, EntityTerminatingEvent>(OnCameraTerminating);

        // Player disconnect
        SubscribeLocalEvent<GlobalSurveillanceCameraMonitorComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GlobalSurveillanceCameraMonitorComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!_states.TryGetValue(uid, out var state))
                continue;

            if (state.Viewers.Count == 0)
                continue;

            state.UpdateTimer += frameTime;
            if (state.UpdateTimer >= UpdateInterval)
            {
                state.UpdateTimer = 0f;
                UpdateUI(uid);
            }
        }
    }

    private void OnStartup(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component, ComponentStartup args)
    {
        _states[uid] = new GlobalCameraState();
    }

    private void OnShutdown(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component, ComponentShutdown args)
    {
        if (_states.TryGetValue(uid, out var state))
        {
            DisconnectCamera(uid, state);
            _states.Remove(uid);
        }
    }

    private void OnMonitorTerminating(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component, ref EntityTerminatingEvent args)
    {
        if (_states.TryGetValue(uid, out var state))
        {
            DisconnectCamera(uid, state);
            _states.Remove(uid);
        }
    }

    private void OnCameraTerminating(EntityUid cameraUid, SurveillanceCameraComponent component, ref EntityTerminatingEvent args)
    {
        var monitorsToUpdate = new List<EntityUid>();

        foreach (var (monitorUid, state) in _states)
        {
            if (state.ActiveCamera == cameraUid)
            {
                DisconnectCamera(monitorUid, state);
                monitorsToUpdate.Add(monitorUid);
            }
        }

        foreach (var monitorUid in monitorsToUpdate)
        {
            UpdateUI(monitorUid);
        }
    }

    private void OnPlayerDetached(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component, PlayerDetachedEvent args)
    {
        if (!_states.TryGetValue(uid, out var state))
            return;

        if (state.Viewers.Remove(args.Entity))
        {
            if (state.Viewers.Count == 0)
                DisconnectCamera(uid, state);
        }
    }

    private void OnPowerChanged(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered && _states.TryGetValue(uid, out var state))
            DisconnectCamera(uid, state);
    }

    private void OnUIOpen(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!_states.TryGetValue(uid, out var state))
            return;

        if (!HasAccess(args.User, uid))
        {
            _userInterface.CloseUi(uid, SurveillanceCameraMonitorUiKey.Key, args.User);
            return;
        }

        if (state.Viewers.Count >= MaxViewersPerMonitor)
        {
            _userInterface.CloseUi(uid, SurveillanceCameraMonitorUiKey.Key, args.User);
            return;
        }

        state.Viewers.Add(args.User);
        UpdateUI(uid);
    }

    private void OnUIClose(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component, BoundUIClosedEvent args)
    {
        if (_states.TryGetValue(uid, out var state))
        {
            state.Viewers.Remove(args.Actor);
            if (state.Viewers.Count == 0)
                DisconnectCamera(uid, state);
        }
    }

    private void OnRefresh(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component,
        SurveillanceCameraRefreshCamerasMessage args)
    {
        UpdateUI(uid);
    }

    private void OnRefresh(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component,
        SurveillanceCameraRefreshSubnetsMessage args)
    {
        UpdateUI(uid);
    }

    private void OnSwitch(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component,
        SurveillanceCameraMonitorSwitchMessage args)
    {
        if (!_states.TryGetValue(uid, out var state))
            return;

        EntityUid? player = null;
        if (state.Viewers.Count > 0)
        {
            var firstViewer = state.Viewers.First();
            if (TryComp<ActorComponent>(firstViewer, out var actor))
                player = actor.PlayerSession.AttachedEntity;
        }

        if (player != null && !HasAccess(player.Value, uid))
            return;

        if (EntityUid.TryParse(args.CameraAddress, out var cameraUid) &&
            Exists(cameraUid) &&
            TryComp<SurveillanceCameraComponent>(cameraUid, out var camera) &&
            camera.Active)
        {
            var cameraXform = Transform(cameraUid);
            state.ActiveGridUid = cameraXform.GridUid;
            SwitchCamera(uid, cameraUid, state);

            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(player)} started viewing camera {ToPrettyString(cameraUid)} from global monitor {ToPrettyString(uid)}");
        }
    }

    private void OnDisconnect(EntityUid uid, GlobalSurveillanceCameraMonitorComponent component,
        SurveillanceCameraDisconnectMessage args)
    {
        if (_states.TryGetValue(uid, out var state))
        {
            DisconnectCamera(uid, state);
            UpdateUI(uid);
        }
    }

    private void SwitchCamera(EntityUid monitorUid, EntityUid cameraUid, GlobalCameraState state)
    {
        if (!Exists(cameraUid) ||
            !TryComp<SurveillanceCameraComponent>(cameraUid, out var cam) ||
            !cam.Active)
            return;

        if (state.ActiveCamera != null && state.ActiveCamera != cameraUid)
        {
            foreach (var viewer in state.Viewers)
            {
                if (TryComp(viewer, out ActorComponent? actor))
                    _viewSubscriberSystem.RemoveViewSubscriber(state.ActiveCamera.Value, actor.PlayerSession);
            }
        }

        foreach (var viewer in state.Viewers)
        {
            if (TryComp(viewer, out ActorComponent? actor))
                _viewSubscriberSystem.AddViewSubscriber(cameraUid, actor.PlayerSession);
        }

        state.ActiveCamera = cameraUid;
        state.ActiveCameraAddress = cameraUid.ToString();
    }

    private void DisconnectCamera(EntityUid monitorUid, GlobalCameraState state)
    {
        if (state.ActiveCamera != null && Exists(state.ActiveCamera.Value))
        {
            foreach (var viewer in state.Viewers)
            {
                if (TryComp(viewer, out ActorComponent? actor))
                    _viewSubscriberSystem.RemoveViewSubscriber(state.ActiveCamera.Value, actor.PlayerSession);
            }
        }
        state.ActiveCamera = null;
        state.ActiveCameraAddress = string.Empty;
        state.ActiveGridUid = null;
    }

    private bool HasAccess(EntityUid user, EntityUid console)
    {
        if (TryComp<AccessReaderComponent>(console, out var accessReader))
            return _accessReader.IsAllowed(user, console, accessReader);
        return true;
    }

    private void UpdateUI(EntityUid uid)
    {
        if (!_states.TryGetValue(uid, out var state))
            return;

        var monitorMapId = Transform(uid).MapID;
        var cameras = GetStationCameras(monitorMapId);
        var subnets = GetSubnets(cameras);

        NetEntity? backgroundGrid = null;
        if (state.ActiveGridUid != null)
            backgroundGrid = GetNetEntity(state.ActiveGridUid.Value);

        var stateData = new GlobalSurveillanceCameraMonitorUiState(
            GetNetEntity(state.ActiveCamera),
            subnets,
            state.ActiveCameraAddress,
            cameras,
            backgroundGrid
        );

        _userInterface.SetUiState(uid, SurveillanceCameraMonitorUiKey.Key, stateData);
    }

    private HashSet<string> GetSubnets(Dictionary<NetEntity, CameraData> cameras)
    {
        var subnets = new HashSet<string>();
        foreach (var (_, data) in cameras)
            subnets.Add(data.SubnetAddress);
        return subnets;
    }

    private Dictionary<NetEntity, CameraData> GetStationCameras(MapId monitorMapId)
    {
        var cameras = new Dictionary<NetEntity, CameraData>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var mapColorIndex = 0;
        var mapColors = new Dictionary<MapId, Color>();

        var query = AllEntityQuery<SurveillanceCameraComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var camera, out var xform))
        {
            if (!camera.Active || xform.MapID == monitorMapId)
                continue;

            if (!mapColors.ContainsKey(xform.MapID))
            {
                mapColors[xform.MapID] = MapColors[mapColorIndex % MapColors.Length];
                mapColorIndex++;
            }

            EntityCoordinates coordinates = EntityCoordinates.Invalid;
            if (xform.GridUid != null)
                coordinates = xform.Coordinates;
            else if (xform.MapUid != null)
                coordinates = new EntityCoordinates(xform.MapUid.Value,
                    _transform.GetWorldPosition(xform, xformQuery));

            var cameraName = camera.UseEntityNameAsCameraId
                ? MetaData(uid).EntityName
                : camera.CameraId;

            var mapName = "Station";
            if (xform.MapUid != null && TryComp(xform.MapUid.Value, out MetaDataComponent? metadata))
                mapName = metadata.EntityName;

            cameras[GetNetEntity(uid)] = new CameraData
            {
                Name = cameraName,
                CameraAddress = uid.ToString(),
                SubnetAddress = mapName,
                SubnetColor = mapColors[xform.MapID],
                Coordinates = GetNetCoordinates(coordinates)
            };
        }
        return cameras;
    }
}
