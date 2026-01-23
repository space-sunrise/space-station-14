using Content.Client.Hands.Systems;
using Content.Shared.Interaction;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
// Starlight Start
<<<<<<< HEAD
using Content.Shared.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
=======
using Robust.Shared.Input;
using Content.Client._Starlight.RCD;
using Robust.Shared.Input.Binding;
using Content.Client.Atmos;
using Content.Shared.Input;
>>>>>>> 7546ce0450 (Better RPD)
// Starlight End

namespace Content.Client.RCD;

/// <summary>
/// System for handling structure ghost placement in places where RCD can create objects.
/// </summary>
public sealed class RCDConstructionGhostSystem : EntitySystem
{
    private const string PlacementMode = nameof(AlignRCDConstruction);
    private const string RpdPlacementMode = nameof(AlignRPDAtmosPipeLayers); // Starlight RPD

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedAtmosPipeLayersSystem _pipeLayers = default!; // Starlight: RPD

    private Direction _placementDirection = default;
    // Starlight Start: RPD
    private bool _useMirrorPrototype = false;
<<<<<<< HEAD
=======
    public event EventHandler? FlipConstructionPrototype;
>>>>>>> 7546ce0450 (Better RPD)

    public override void Initialize()
    {
        base.Initialize();

<<<<<<< HEAD
        // Bind flip key
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.FlipObject,
=======
        // bind key
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.EditorFlipObject,
>>>>>>> 7546ce0450 (Better RPD)
                new PointerInputCmdHandler(HandleFlip, outsidePrediction: true))
            .Register<RCDConstructionGhostSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<RCDConstructionGhostSystem>();
        base.Shutdown();
    }

    private bool HandleFlip(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
<<<<<<< HEAD
        // Only act on key down
        if (args.State != BoundKeyState.Down)
            return false;

        // Only operate when placement is active and not erasing
        if (!_placementManager.IsActive || _placementManager.Eraser)
            return false;

        var placerEntity = _placementManager.CurrentPermission?.MobUid;

        // Must be an RCD placer
        if (!TryComp<RCDComponent>(placerEntity, out var rcd))
            return false;

        // Check if there is a mirror available
        var proto = _protoManager.Index(rcd.ProtoId);

        if (string.IsNullOrEmpty(proto.MirrorPrototype))
            return false;

        // Toggle mirror
        _useMirrorPrototype = !_useMirrorPrototype;

        // Determine the prototype
        var useProto = _useMirrorPrototype && !string.IsNullOrEmpty(proto.MirrorPrototype)
            ? proto.MirrorPrototype
            : proto.Prototype;

        // Recreate the placer
        if (placerEntity != null)
            CreatePlacer(placerEntity.Value, useProto, proto.Mode == RcdMode.ConstructTile);

        // Tell the server so server
        RaiseNetworkEvent(new RCDConstructionGhostFlipEvent(GetNetEntity(placerEntity ?? EntityUid.Invalid), _useMirrorPrototype));

        return true;
    }
    // Starlight End
=======
        if (args.State == BoundKeyState.Down)
        {
            if (!_placementManager.IsActive || _placementManager.Eraser)
                return false;

            var placerEntity = _placementManager.CurrentPermission?.MobUid;

            if (!TryComp<RCDComponent>(placerEntity, out var rcd) ||
                string.IsNullOrEmpty(rcd.CachedPrototype.MirrorPrototype))
                return false;

            _useMirrorPrototype = !rcd.UseMirrorPrototype;

            // tell the server

            RaiseNetworkEvent(new RCDConstructionGhostFlipEvent(GetNetEntity(placerEntity.Value), _useMirrorPrototype));
        }

        return true;
    }
    // Starlight End: RPD
>>>>>>> 7546ce0450 (Better RPD)

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsRCD = HasComp<RCDComponent>(placerEntity);

        // Exit if erasing or the current placer is not an RCD (build mode is active)
        if (_placementManager.Eraser || (placerEntity != null && !placerIsRCD))
            return;

        // Determine if player is carrying an RCD in their active hand
        if (_playerManager.LocalSession?.AttachedEntity is not { } player)
            return;

        var heldEntity = _hands.GetActiveItem(player);

        // Don't open the placement overlay for client-side RCDs.
        // This may happen when predictively spawning one in your hands.
        if (heldEntity != null && IsClientSide(heldEntity.Value))
            return;

        if (!TryComp<RCDComponent>(heldEntity, out var rcd))
        {
            // If the player was holding an RCD, but is no longer, cancel placement
            if (placerIsRCD)
                _placementManager.Clear();

            return;
        }
        // Starlight edit Start: RPD - use the mirrored prototype if the flip state is toggled on
        // var prototype = _protoManager.Index(rcd.ProtoId);

        // Determine if mirrored
        var cachedProto = rcd.CachedPrototype;
        var wantMirror = _useMirrorPrototype && !string.IsNullOrEmpty(cachedProto.MirrorPrototype);
        var prototype = wantMirror ? cachedProto.MirrorPrototype : cachedProto.Prototype;

        bool isLayered = rcd.IsRpd
            && _protoManager.TryIndex<RCDPrototype>(cachedProto.ID, out var rcdProto)
            && rcdProto.HasLayers;

        var desiredMode = isLayered ? RpdPlacementMode : PlacementMode;
        // Starlight edit End: RPD - use the mirrored prototype if the flip state is toggled on

        // Update the direction the RCD prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new RCDConstructionGhostRotationEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        // If the placer has not changed, exit
<<<<<<< HEAD
        // Starlight edit Start: RPD
        var effectiveProto = _useMirrorPrototype && !string.IsNullOrEmpty(prototype.MirrorPrototype)
            ? prototype.MirrorPrototype
            : prototype.Prototype;

        effectiveProto = ApplyPipeLayerPrototype(rcd, effectiveProto);

        if (heldEntity == placerEntity && effectiveProto == placerProto)
=======
        // Starlight edit Start
        if (heldEntity == placerEntity &&
            prototype == placerProto &&
            _placementManager.CurrentPermission?.PlacementOption == desiredMode)
>>>>>>> 7546ce0450 (Better RPD)
        // Starlight edit End
            return;

        // Create a new placer
    // Starlight Start: RPD
        CreatePlacer(heldEntity.Value, effectiveProto, prototype.Mode == RcdMode.ConstructTile);
    }

    private void CreatePlacer(EntityUid uid, string? entityType, bool isTile)
    {
    // Starlight End
        var newObjInfo = new PlacementInformation
        {
<<<<<<< HEAD
            MobUid = uid, // Starlight Edit
            PlacementOption = PlacementMode,
            EntityType = entityType, // Starlight Edit
            Range = (int)Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = isTile, // Starlight Edit
=======
            MobUid = heldEntity.Value,
            PlacementOption = desiredMode, // Starlight Edit: PlacementMode -> desiredMode
            EntityType = prototype, // Starlight Edit: prototype.Prototype -> prototype
            Range = (int)Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = (cachedProto.Mode == RcdMode.ConstructTile), // Starlight Edit: prototype.Mode -> cachedProto.Mode
>>>>>>> 7546ce0450 (Better RPD)
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
    // Starlight Start: RPD
    private string? ApplyPipeLayerPrototype(RCDComponent rcd, string? entityType)
    {
        if (!rcd.IsRPD || string.IsNullOrEmpty(entityType))
            return entityType;

        if (!_protoManager.TryIndex<EntityPrototype>(entityType, out var entityProto))
            return entityType;

        if (!entityProto.TryGetComponent(out AtmosPipeLayersComponent? atmosLayers, EntityManager.ComponentFactory))
            return entityType;

        return _pipeLayers.TryGetAlternativePrototype(atmosLayers, rcd.SelectedPipeLayer, out var altProto)
            ? altProto
            : entityType;
    }
    // Starlight End
}
