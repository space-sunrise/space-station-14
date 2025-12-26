using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Implants;
using Content.Server.Implants; // for concrete SubdermalImplantSystem
using Content.Shared.Implants.Components;
using Content.Shared.Eui;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Organ;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Server._Starlight.Medical.Limbs;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.Console;

namespace Content.Server.Administration.UI;

/// <summary>
/// Server side EUI for managing implants on a target entity.
/// </summary>
public sealed class ImplantAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    // Don't inject SubdermalImplantSystem directly (not registered for arbitrary object injection); resolve via IEntityManager when needed.
    [Dependency] private readonly IConGroupController _groupController = default!;
    // LimbSystem can't be injected here (EUI not a system & system types not registered for arbitrary IoC); resolve via _entManager.System<LimbSystem>() when needed.

    private readonly NetEntity _target;

    public ImplantAdminEui(NetEntity target)
    {
        _target = target;
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
        _adminManager.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _adminManager.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
    if (args.Player == Player && !_groupController.CanCommand(Player, "vv"))
            Close();
    }

    public override EuiStateBase GetNewState()
    {
        var state = new ImplantAdminEuiState
        {
            TargetNetEntity = _target,
        };

    if (_entManager.TryGetEntity(_target, out var uid) && uid != null && _entManager.TryGetComponent(uid.Value, out ImplantedComponent? implanted))
        {
            foreach (var ent in implanted.ImplantContainer.ContainedEntities.ToList())
            {
                if (!_entManager.EntityExists(ent) || !_entManager.TryGetComponent(ent, out SubdermalImplantComponent? implantComp))
                    continue;

                var entry = new ImplantEntry
                {
                    NetEntity = _entManager.GetNetEntity(ent),
                    Permanent = implantComp.Permanent,
                };

                if (_entManager.TryGetComponent(ent, out MetaDataComponent? meta))
                {
                    entry.Name = meta.EntityName;
                    var protoId = meta.EntityPrototype?.ID;
                    if (protoId != null && _prototypeManager.TryIndex<EntityPrototype>(protoId, out var proto))
                    {
                        entry.ProtoId = proto.ID;
                        if (string.IsNullOrEmpty(entry.Name))
                            entry.Name = proto.Name;
                    }
                }
                state.Implants.Add(entry);
            }
        }

        // Body parts & organs enumeration
        if (_entManager.TryGetEntity(_target, out var bodyUid) && bodyUid != null && _entManager.TryGetComponent(bodyUid.Value, out BodyComponent? body))
        {
            // Traverse root part tree to collect part slot containers
            var sharedBody = _entManager.System<SharedBodySystem>();
            // We need to walk all BodyPartComponents that belong to this body to list child slot availability.
            // Simpler: iterate all BodyPartComponents with this body and inspect their Children / Organs dictionaries.
            var query = _entManager.EntityQueryEnumerator<BodyPartComponent>();
            while (query.MoveNext(out var partUid, out var partComp))
            {
                if (partComp.Body != bodyUid)
                    continue;

                // Ensure expected child slots (hands / feet) exist for arms / legs so they show up even if removed previously.
                // New logic: don't rely solely on symmetry metadata (may be None / unset in some prototypes); also accept generic names.
                // Build a temporary list of child part slot ids via system helper (avoids direct Children access for analyzer compliance)
                var partSlotIds = sharedBody.EnumeratePartSlots(partUid, partComp).ToList();

                if (partComp.PartType == BodyPartType.Arm || partComp.PartType == BodyPartType.Leg)
                {
                    bool hasRequired = partComp.PartType == BodyPartType.Arm
                        ? partSlotIds.Any(p => p.SlotId.Contains("hand"))
                        : partSlotIds.Any(p => p.SlotId.Contains("foot"));
                    if (!hasRequired)
                    {
                        var expectedType = partComp.PartType == BodyPartType.Arm ? BodyPartType.Hand : BodyPartType.Foot;
                        string preferred = partComp.PartType == BodyPartType.Arm
                            ? (partComp.Symmetry == BodyPartSymmetry.Left ? "left hand" : partComp.Symmetry == BodyPartSymmetry.Right ? "right hand" : "hand")
                            : (partComp.Symmetry == BodyPartSymmetry.Left ? "left foot" : partComp.Symmetry == BodyPartSymmetry.Right ? "right foot" : "foot");
                        if (!sharedBody.TryCreatePartSlot(partUid, preferred, expectedType, out _))
                        {
                            if (partComp.PartType == BodyPartType.Arm)
                            {
                                if (!partSlotIds.Any(p => p.SlotId == "left hand")) sharedBody.TryCreatePartSlot(partUid, "left hand", expectedType, out _);
                                if (!partSlotIds.Any(p => p.SlotId == "right hand")) sharedBody.TryCreatePartSlot(partUid, "right hand", expectedType, out _);
                                if (!partSlotIds.Any(p => p.SlotId == "hand")) sharedBody.TryCreatePartSlot(partUid, "hand", expectedType, out _);
                            }
                            else
                            {
                                if (!partSlotIds.Any(p => p.SlotId == "left foot")) sharedBody.TryCreatePartSlot(partUid, "left foot", expectedType, out _);
                                if (!partSlotIds.Any(p => p.SlotId == "right foot")) sharedBody.TryCreatePartSlot(partUid, "right foot", expectedType, out _);
                                if (!partSlotIds.Any(p => p.SlotId == "foot")) sharedBody.TryCreatePartSlot(partUid, "foot", expectedType, out _);
                            }
                        }
                        // Refresh list after potential creation
                        partSlotIds = sharedBody.EnumeratePartSlots(partUid, partComp).ToList();
                    }
                }

                // Child part slots
                foreach (var (slotId, data) in partSlotIds)
                {
                    var containerId = SharedBodySystem.GetPartSlotContainerId(slotId);
                    BaseContainer? container = null;
                    if (_entManager.TryGetComponent(partUid, out ContainerManagerComponent? contMan) &&
                        contMan.TryGetContainer(containerId, out var found))
                    {
                        container = found;
                    }
                    else
                    {
                        // Ensure the container exists so the slot is visible even if never touched yet.
                        // (BodySystem lazily creates these; admins expect to see all defined slots.)
                        try
                        {
                            container = _entManager.System<SharedContainerSystem>()
                                .EnsureContainer<ContainerSlot>(partUid, containerId);
                        }
                        catch
                        {
                            // Swallow; purely best-effort for visibility.
                        }
                    }

                    var occupied = container != null && container.ContainedEntities.Count > 0;
                    NetEntity? installedNet = null;
                    string protoId = string.Empty;
                    string name = string.Empty;
                    if (occupied)
                    {
                        var installed = container!.ContainedEntities[0];
                        installedNet = _entManager.GetNetEntity(installed);
                        if (_entManager.TryGetComponent(installed, out MetaDataComponent? meta))
                        {
                            protoId = meta.EntityPrototype?.ID ?? string.Empty;
                            name = string.IsNullOrEmpty(meta.EntityName) && meta.EntityPrototype != null ? meta.EntityPrototype.Name : meta.EntityName;
                        }
                    }
                    state.PartSlots.Add(new BodySlotEntry
                    {
                        SlotId = slotId,
                        IsOrgan = false,
                        Occupied = occupied,
                        Entity = installedNet,
                        ProtoId = protoId,
                        Name = name,
                    });
                }

                // Organ slots
                foreach (var (slotId, data) in sharedBody.EnumerateOrganSlots(partUid, partComp))
                {
                    var containerId = SharedBodySystem.GetOrganContainerId(slotId);
                    BaseContainer? container = null;
                    if (_entManager.TryGetComponent(partUid, out ContainerManagerComponent? contMan2) &&
                        contMan2.TryGetContainer(containerId, out var found2))
                    {
                        container = found2;
                    }
                    else
                    {
                        // Same rationale as for part slots: force-create empty container for visibility.
                        try
                        {
                            container = _entManager.System<SharedContainerSystem>()
                                .EnsureContainer<ContainerSlot>(partUid, containerId);
                        }
                        catch
                        {
                            // Ignore failures (e.g. invalid slot id) to avoid breaking UI.
                        }
                    }

                    var occupied = container != null && container.ContainedEntities.Count > 0;
                    NetEntity? installedNet = null;
                    string protoId = string.Empty;
                    string name = string.Empty;
                    if (occupied)
                    {
                        var installed = container!.ContainedEntities[0];
                        installedNet = _entManager.GetNetEntity(installed);
                        if (_entManager.TryGetComponent(installed, out MetaDataComponent? meta))
                        {
                            protoId = meta.EntityPrototype?.ID ?? string.Empty;
                            name = string.IsNullOrEmpty(meta.EntityName) && meta.EntityPrototype != null ? meta.EntityPrototype.Name : meta.EntityName;
                        }
                    }
                    state.OrganSlots.Add(new BodySlotEntry
                    {
                        SlotId = slotId,
                        IsOrgan = true,
                        Occupied = occupied,
                        Entity = installedNet,
                        ProtoId = protoId,
                        Name = name,
                    });
                }
            }
        }


        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

    if (!_groupController.CanCommand(Player, "vv"))
            return;

    if (!_entManager.TryGetEntity(_target, out var targetUid) || targetUid == null)
            return;

        switch (msg)
        {
            case ImplantAdminAddMessage add:
                if (string.IsNullOrEmpty(add.ProtoId) || !_prototypeManager.HasIndex<EntityPrototype>(add.ProtoId))
                    return;
                _entManager.System<SubdermalImplantSystem>().AddImplant(targetUid.Value, add.ProtoId);
                StateDirty();
                break;
            case ImplantAdminRemoveMessage rem:
                if (!_entManager.TryGetEntity(rem.ImplantNetEntity, out var implantUid) || implantUid == null)
                    return;
                if (!_entManager.TryGetComponent(targetUid.Value, out ImplantedComponent? implanted) || !implanted.ImplantContainer.Contains(implantUid.Value))
                    return;
                if (_entManager.TryGetComponent(implantUid.Value, out SubdermalImplantComponent? implantComp))
                {
                    _entManager.System<SubdermalImplantSystem>().ForceRemove((targetUid.Value, implanted), implantUid.Value);
                    StateDirty();
                }
                break;
            case ImplantAdminReplaceBodySlotMessage rep:
                HandleReplaceBodySlot(targetUid.Value, rep);
                break;
            case ImplantAdminRemoveBodySlotMessage rslot:
                HandleRemoveBodySlot(targetUid.Value, rslot);
                break;
        }
    }

    private void HandleReplaceBodySlot(EntityUid bodyUid, ImplantAdminReplaceBodySlotMessage msg)
    {
        if (string.IsNullOrEmpty(msg.SlotId) || string.IsNullOrEmpty(msg.ProtoId) || !_prototypeManager.HasIndex<EntityPrototype>(msg.ProtoId))
            return;
        // spawn new part / organ and insert. If occupied remove first.
    if (!_entManager.TryGetComponent(bodyUid, out BodyComponent? body))
            return;

        // We need to find a container with the matching slot (any parent part). Iterate parts on this body.
        var sharedBody = _entManager.System<SharedBodySystem>();

        // First pass: locate the parent part & container without mutating anything (avoid collection modification during enumeration).
        EntityUid parentPartUid = default;
        BodyPartComponent? parentPart = null;
        ContainerManagerComponent? parentContMan = null;
        var query = _entManager.EntityQueryEnumerator<BodyPartComponent>();
        while (query.MoveNext(out var partUid, out var partComp))
        {
            if (partComp.Body != bodyUid)
                continue;
            if (!_entManager.TryGetComponent(partUid, out ContainerManagerComponent? contManTemp))
                continue;

            bool hasSlotLocal;
            if (msg.IsOrgan)
            {
                var organs = partComp.Organs;
                hasSlotLocal = organs.ContainsKey(msg.SlotId);
            }
            else
            {
                var children = partComp.Children;
                hasSlotLocal = children.ContainsKey(msg.SlotId);
                if (!hasSlotLocal)
                {
                    var lower = msg.SlotId.ToLowerInvariant();
                    if (partComp.PartType == BodyPartType.Arm && lower.Contains("hand"))
                    {
                        if (sharedBody.TryCreatePartSlot(partUid, msg.SlotId, BodyPartType.Hand, out _))
                            hasSlotLocal = true;
                    }
                    else if (partComp.PartType == BodyPartType.Leg && lower.Contains("foot"))
                    {
                        if (sharedBody.TryCreatePartSlot(partUid, msg.SlotId, BodyPartType.Foot, out _))
                            hasSlotLocal = true;
                    }
                }
            }

            if (!hasSlotLocal)
                continue;

            parentPartUid = partUid;
            parentPart = partComp;
            parentContMan = contManTemp;
            break;
        }

        if (parentPart == null || parentContMan == null)
            return;

        // Now perform mutations using the located parent part.
        var containerIdFound = msg.IsOrgan ? SharedBodySystem.GetOrganContainerId(msg.SlotId) : SharedBodySystem.GetPartSlotContainerId(msg.SlotId);
        if (!parentContMan.TryGetContainer(containerIdFound, out var container))
            return;

        // Remove existing occupant first.
        if (container.ContainedEntities.Count > 0)
        {
            var existing = container.ContainedEntities[0];
            if (msg.IsOrgan)
            {
                if (_entManager.TryGetComponent(existing, out OrganComponent? organComp))
                {
                    var extractedEv = new SurgeryOrganExtracted(bodyUid, parentPartUid, existing);
                    _entManager.EventBus.RaiseLocalEvent(existing, ref extractedEv, broadcast: true);
                    sharedBody.RemoveOrgan(existing, organComp);
                }
            }
            else if (_entManager.TryGetComponent(existing, out BodyPartComponent? existingPart))
            {
                if (_entManager.TryGetComponent(bodyUid, out HumanoidAppearanceComponent? humanoid)
                    && _entManager.TryGetComponent(existing, out TransformComponent? limbXform)
                    && _entManager.TryGetComponent(existing, out MetaDataComponent? limbMeta)
                    && _entManager.TryGetComponent(bodyUid, out TransformComponent? bodyXform)
                    && _entManager.TryGetComponent(bodyUid, out BodyComponent? bodyComp2))
                {
                    var limbSystem = _entManager.System<LimbSystem>();
                    limbSystem.Amputatate((bodyUid, bodyXform!, humanoid!, bodyComp2!), (existing, limbXform!, limbMeta!, existingPart));
                    if (_entManager.EntityExists(existing))
                        _entManager.QueueDeleteEntity(existing);
                }
                else
                {
                    sharedBody.RemovePartPublic(parentPartUid, msg.SlotId);
                    _entManager.QueueDeleteEntity(existing);
                }
            }
            else
            {
                _entManager.System<SharedContainerSystem>().Remove(existing, container);
                _entManager.QueueDeleteEntity(existing);
            }
        }

        // Ensure slot exists post-amputation (custom/virtual limb may have removed it).
        if (!msg.IsOrgan)
        {
            var childrenAfter = parentPart.Children;
            if (!childrenAfter.ContainsKey(msg.SlotId))
            {
                var lower = msg.SlotId.ToLowerInvariant();
                if (parentPart.PartType == BodyPartType.Arm && lower.Contains("hand"))
                    sharedBody.TryCreatePartSlot(parentPartUid, msg.SlotId, BodyPartType.Hand, out _);
                else if (parentPart.PartType == BodyPartType.Leg && lower.Contains("foot"))
                    sharedBody.TryCreatePartSlot(parentPartUid, msg.SlotId, BodyPartType.Foot, out _);

                // Refresh container ref if newly created.
                if (parentContMan.TryGetContainer(SharedBodySystem.GetPartSlotContainerId(msg.SlotId), out var recreated))
                    container = recreated;
            }
        }

        var protoFound = _prototypeManager.Index<EntityPrototype>(msg.ProtoId);
        var spawnedPart = _entManager.SpawnEntity(protoFound.ID, _entManager.GetComponent<TransformComponent>(bodyUid).Coordinates);

        if (msg.IsOrgan)
        {
            if (_entManager.TryGetComponent(spawnedPart, out OrganComponent? organComp2))
            {
                if (!sharedBody.InsertOrgan(parentPartUid, spawnedPart, msg.SlotId))
                {
                    _entManager.QueueDeleteEntity(spawnedPart);
                    return;
                }
                var implantEv = new SurgeryOrganImplantationCompleted(bodyUid, parentPartUid, spawnedPart);
                _entManager.EventBus.RaiseLocalEvent(spawnedPart, ref implantEv, broadcast: true);
            }
            else
            {
                _entManager.QueueDeleteEntity(spawnedPart);
                return;
            }
        }
        else
        {
            if (!_entManager.TryGetComponent(spawnedPart, out BodyPartComponent? newPart2))
            {
                if (_entManager.TryGetComponent(bodyUid, out HumanoidAppearanceComponent? humanoidForItem)
                    && _entManager.TryGetComponent(parentPartUid, out BodyPartComponent? parentPartForItem2)
                    && _entManager.TryGetComponent(spawnedPart, out MetaDataComponent? meta2))
                {
                    var limbSystem = _entManager.System<LimbSystem>();
                    if (limbSystem.AttachItem(bodyUid, msg.SlotId, (parentPartUid, parentPartForItem2!), (spawnedPart, meta2)))
                    {
                        StateDirty();
                        return;
                    }
                }
                _entManager.QueueDeleteEntity(spawnedPart);
                return;
            }
            var childrenDict2 = parentPart.Children;
            if (!childrenDict2.TryGetValue(msg.SlotId, out var slotData2) || slotData2.Type != newPart2.PartType)
            {
                _entManager.QueueDeleteEntity(spawnedPart);
                return;
            }
            bool attached2 = false;
            if (_entManager.TryGetComponent(bodyUid, out HumanoidAppearanceComponent? humanoidAttach)
                && _entManager.TryGetComponent(parentPartUid, out BodyPartComponent? parentPartAttach)
                && _entManager.TryGetComponent(spawnedPart, out BodyPartComponent? limbPart2))
            {
                var limbSystem = _entManager.System<LimbSystem>();
                attached2 = limbSystem.AttachLimb((bodyUid, humanoidAttach!), msg.SlotId, (parentPartUid, parentPartAttach!), (spawnedPart, limbPart2!));
            }
            if (!attached2)
            {
                if (!sharedBody.CanAttachPart(parentPartUid, msg.SlotId, spawnedPart) || !sharedBody.AttachPart(parentPartUid, msg.SlotId, spawnedPart))
                {
                    _entManager.QueueDeleteEntity(spawnedPart);
                    return;
                }
            }
        }

        StateDirty();
        return;
    }

    private void HandleRemoveBodySlot(EntityUid bodyUid, ImplantAdminRemoveBodySlotMessage msg)
    {
        if (string.IsNullOrEmpty(msg.SlotId))
            return;

        if (!_entManager.TryGetComponent(bodyUid, out BodyComponent? body))
            return;

        var sharedBody = _entManager.System<SharedBodySystem>();
        EntityUid parentPartUid = default;
        BodyPartComponent? parentPart = null;
        ContainerManagerComponent? parentContMan = null;
        var query = _entManager.EntityQueryEnumerator<BodyPartComponent>();
        while (query.MoveNext(out var partUid, out var partComp))
        {
            if (partComp.Body != bodyUid)
                continue;
            if (!_entManager.TryGetComponent(partUid, out ContainerManagerComponent? contManTemp))
                continue;
            bool hasSlotLocal;
            if (msg.IsOrgan)
            {
                var organs = partComp.Organs;
                hasSlotLocal = organs.ContainsKey(msg.SlotId);
            }
            else
            {
                var children = partComp.Children;
                hasSlotLocal = children.ContainsKey(msg.SlotId);
            }
            if (!hasSlotLocal)
                continue;
            parentPartUid = partUid;
            parentPart = partComp;
            parentContMan = contManTemp;
            break;
        }

        if (parentPart == null || parentContMan == null)
            return;

        var containerId2 = msg.IsOrgan ? SharedBodySystem.GetOrganContainerId(msg.SlotId) : SharedBodySystem.GetPartSlotContainerId(msg.SlotId);
        if (!parentContMan.TryGetContainer(containerId2, out var container2))
            return;
        if (container2.ContainedEntities.Count == 0)
            return;
        var existing2 = container2.ContainedEntities[0];
        if (msg.IsOrgan && _entManager.TryGetComponent(existing2, out OrganComponent? organComp2))
        {
            var extractedEv2 = new SurgeryOrganExtracted(bodyUid, parentPartUid, existing2);
            _entManager.EventBus.RaiseLocalEvent(existing2, ref extractedEv2, broadcast: true);
            sharedBody.RemoveOrgan(existing2, organComp2);
        }
        else if (_entManager.TryGetComponent(existing2, out BodyPartComponent? existingPart2))
        {
            if (_entManager.TryGetComponent(bodyUid, out HumanoidAppearanceComponent? humanoid2)
                && _entManager.TryGetComponent(existing2, out TransformComponent? limbXform2)
                && _entManager.TryGetComponent(existing2, out MetaDataComponent? limbMeta2)
                && _entManager.TryGetComponent(bodyUid, out TransformComponent? bodyXform2)
                && _entManager.TryGetComponent(bodyUid, out BodyComponent? bodyComp3))
            {
                var limbSystem = _entManager.System<LimbSystem>();
                limbSystem.Amputatate((bodyUid, bodyXform2!, humanoid2!, bodyComp3!), (existing2, limbXform2!, limbMeta2!, existingPart2));
                if (_entManager.EntityExists(existing2))
                    _entManager.QueueDeleteEntity(existing2);
            }
            else
            {
                sharedBody.RemovePartPublic(parentPartUid, msg.SlotId);
                _entManager.QueueDeleteEntity(existing2);
            }
        }
        else
        {
            _entManager.System<SharedContainerSystem>().Remove(existing2, container2);
            _entManager.QueueDeleteEntity(existing2);
        }
        StateDirty();
        return;
    }
}
