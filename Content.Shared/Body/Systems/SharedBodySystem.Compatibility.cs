using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part;
using Content.Shared.Gibbing;
using Robust.Shared.Containers;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Compatibility layer for fork systems that still use the pre-Nubody body-part API.
/// </summary>
public abstract partial class SharedBodySystem : EntitySystem
{
    public const string BodyRootContainerId = BodyComponent.ContainerID;
    public const string PartSlotContainerIdPrefix = "bodypart-";
    public const string OrganSlotContainerIdPrefix = "organ-";

    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<OrganComponent> _organQuery;
    private EntityQuery<BodyPartComponent> _partQuery;

    public override void Initialize()
    {
        base.Initialize();

        _bodyQuery = GetEntityQuery<BodyComponent>();
        _organQuery = GetEntityQuery<OrganComponent>();
        _partQuery = GetEntityQuery<BodyPartComponent>();

        SubscribeLocalEvent<BodyPartComponent, OrganGotInsertedEvent>(OnPartInserted);
        SubscribeLocalEvent<BodyPartComponent, OrganGotRemovedEvent>(OnPartRemoved);
    }

    public static string GetPartSlotContainerId(string slotId) => PartSlotContainerIdPrefix + slotId;

    public static string GetOrganContainerId(string slotId) => OrganSlotContainerIdPrefix + slotId;

    private void OnPartInserted(Entity<BodyPartComponent> ent, ref OrganGotInsertedEvent args)
    {
        ent.Comp.Body = args.Target;
        Dirty(ent);
    }

    private void OnPartRemoved(Entity<BodyPartComponent> ent, ref OrganGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        Dirty(ent);
    }

    public HashSet<EntityUid> GibBody(
        EntityUid bodyId,
        bool gibOrgans = false,
        BodyComponent? body = null)
    {
        return _gibbing.Gib(bodyId);
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(
        EntityUid? bodyId,
        BodyComponent? body = null)
    {
        if (bodyId == null || !_bodyQuery.Resolve(bodyId.Value, ref body, logMissing: false))
            yield break;

        foreach (var organ in body.Organs?.ContainedEntities ?? [])
        {
            if (_partQuery.TryComp(organ, out var part))
                yield return (organ, part);
        }
    }

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetBodyOrgans(
        EntityUid? bodyId,
        BodyComponent? body = null)
    {
        if (bodyId == null || !_bodyQuery.Resolve(bodyId.Value, ref body, logMissing: false))
            yield break;

        foreach (var organ in body.Organs?.ContainedEntities ?? [])
        {
            if (_organQuery.TryComp(organ, out var organComp))
                yield return (organ, organComp);
        }
    }

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetPartOrgans(
        EntityUid partId,
        BodyPartComponent? part = null)
    {
        if (!_partQuery.Resolve(partId, ref part, logMissing: false)
            || part.Body == null
            || !_bodyQuery.TryComp(part.Body.Value, out var body))
        {
            yield break;
        }

        foreach (var organ in body.Organs?.ContainedEntities ?? [])
        {
            if (organ == partId
                || !_organQuery.TryComp(organ, out var organComp)
                || _partQuery.HasComp(organ)
                || !IsOrganForPart(part.PartType, organComp))
            {
                continue;
            }

            yield return (organ, organComp);
        }
    }

    public List<Entity<T, OrganComponent>> GetBodyOrganEntityComps<T>(Entity<BodyComponent?> entity)
        where T : IComponent
    {
        var list = new List<Entity<T, OrganComponent>>();
        if (!_bodyQuery.Resolve(entity, ref entity.Comp))
            return list;

        var query = GetEntityQuery<T>();
        foreach (var (organ, organComp) in GetBodyOrgans(entity.Owner, entity.Comp))
        {
            if (query.TryComp(organ, out var comp))
                list.Add((organ, comp, organComp));
        }

        return list;
    }

    public bool TryGetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity,
        [NotNullWhen(true)] out List<Entity<T, OrganComponent>>? comps)
        where T : IComponent
    {
        comps = GetBodyOrganEntityComps<T>(entity);
        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }

    public List<Entity<T, OrganComponent>> GetBodyOrganEntityComps<T>(
        EntityUid bodyId,
        BodyComponent? body = null)
        where T : IComponent
    {
        return GetBodyOrganEntityComps<T>((bodyId, body));
    }

    public bool TryGetBodyOrganEntityComps<T>(
        EntityUid bodyId,
        [NotNullWhen(true)] out List<Entity<T, OrganComponent>>? comps,
        BodyComponent? body = null)
        where T : IComponent
    {
        return TryGetBodyOrganEntityComps((bodyId, body), out comps);
    }

    public bool TryGetOrgansWithComponent<TComp>(
        Entity<BodyComponent?> ent,
        out List<Entity<TComp>> organs)
        where TComp : Component
    {
        organs = new();
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        var query = GetEntityQuery<TComp>();
        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            if (query.TryComp(organ, out var comp))
                organs.Add((organ, comp));
        }

        return organs.Count != 0;
    }

    public bool RemoveOrgan(EntityUid organId, OrganComponent? organ = null)
    {
        if (!_organQuery.Resolve(organId, ref organ, logMissing: false)
            || organ.Body == null
            || !_bodyQuery.TryComp(organ.Body.Value, out var body)
            || body.Organs == null)
        {
            return false;
        }

        return Containers.Remove(organId, body.Organs);
    }

    public bool InsertOrgan(
        EntityUid partId,
        EntityUid organId,
        string slotId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null)
    {
        if (!_partQuery.Resolve(partId, ref part, logMissing: false)
            || part.Body == null
            || !_bodyQuery.TryComp(part.Body.Value, out var body)
            || body.Organs == null
            || !_organQuery.Resolve(organId, ref organ, logMissing: false))
        {
            return false;
        }

        return Containers.Insert(organId, body.Organs);
    }

    public bool AttachPart(
        Entity<BodyPartComponent> parent,
        string slot,
        Entity<BodyPartComponent> child)
    {
        if (!parent.Comp.Children.ContainsKey(slot))
            TryCreatePartSlot(parent, slot, child.Comp.PartType, out _);

        if (!Containers.TryGetContainer(parent, GetPartSlotContainerId(slot), out var container))
            return false;

        if (!Containers.Insert(child.Owner, container))
            return false;

        child.Comp.Body = parent.Comp.Body;
        Dirty(child);
        return true;
    }

    public bool AttachPart(
        EntityUid parent,
        string slot,
        EntityUid child,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? childPart = null)
    {
        return _partQuery.Resolve(parent, ref parentPart, logMissing: false)
            && _partQuery.Resolve(child, ref childPart, logMissing: false)
            && AttachPart((parent, parentPart), slot, (child, childPart));
    }

    public bool TryCreatePartSlot(
        Entity<BodyPartComponent> partEnt,
        string slotId,
        BodyPartType partType,
        [NotNullWhen(true)] out string? createdSlotId)
    {
        var slot = new BodyPartSlot(slotId, partType);
        partEnt.Comp.Children[slotId] = slot;
        Containers.EnsureContainer<ContainerSlot>(partEnt, GetPartSlotContainerId(slotId));
        Dirty(partEnt);
        createdSlotId = slotId;
        return true;
    }

    public bool TryGetFreePartSlot(
        EntityUid partId,
        [NotNullWhen(true)] out string? freeSlotId,
        BodyPartComponent? part = null)
    {
        freeSlotId = null;
        if (!_partQuery.Resolve(partId, ref part, logMissing: false))
            return false;

        foreach (var slot in part.Children.Keys)
        {
            if (Containers.TryGetContainer(partId, GetPartSlotContainerId(slot), out var container)
                && container.ContainedEntities.Count != 0)
            {
                continue;
            }

            freeSlotId = slot;
            return true;
        }

        return false;
    }

    public IEnumerable<EntityUid> GetBodyPartAdjacentParts(
        EntityUid partId,
        BodyPartComponent? part = null)
    {
        if (!_partQuery.Resolve(partId, ref part, logMissing: false))
            yield break;

        foreach (var slot in part.Children.Keys)
        {
            if (!Containers.TryGetContainer(partId, GetPartSlotContainerId(slot), out var container))
                continue;

            foreach (var child in container.ContainedEntities)
                yield return child;
        }
    }

    public (EntityUid Parent, string Slot)? GetParentPartAndSlotOrNull(EntityUid partId)
    {
        if (!Containers.TryGetContainingContainer((partId, null, null), out var container))
            return null;

        var containerId = container.ID;
        if (!containerId.StartsWith(PartSlotContainerIdPrefix, StringComparison.Ordinal))
            return null;

        return (container.Owner, containerId[PartSlotContainerIdPrefix.Length..]);
    }

    private static bool IsOrganForPart(BodyPartType partType, OrganComponent organ)
    {
        var category = organ.Category?.ToString();
        return partType switch
        {
            BodyPartType.Head => category is "Brain" or "Eyes" or "Tongue" or "Ears",
            BodyPartType.Torso => category is "Appendix" or "Lungs" or "Heart" or "Stomach" or "Liver" or "Kidneys",
            _ => false,
        };
    }
}
