using Content.Shared._Sunrise.CollectiveMind;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CollectiveMind;

public sealed class CollectiveMindSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CollectiveMindGroupComponent, ComponentShutdown>(OnGroupShutdown);
        SubscribeLocalEvent<CollectiveMindComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<CollectiveMindComponent> ent, ref ComponentGetState args)
    {
        var memberships = new List<CollectiveMindNetworkMembership>(ent.Comp.Memberships.Count);
        foreach (var membership in ent.Comp.Memberships)
        {
            memberships.Add(new CollectiveMindNetworkMembership(membership.Mind, membership.Permissions));
        }

        args.State = new CollectiveMindComponentState(memberships);
    }

    #region Group management

    private void OnGroupShutdown(Entity<CollectiveMindGroupComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<CollectiveMindComponent>();

        /// "Член-ство" хранит EntityUid группы -> удаляем осиротевшие записи вместе с компонентом группы
        while (query.MoveNext(out var uid, out var collectiveMind))
        {
            if (collectiveMind.Memberships.RemoveAll(x => x.Group == ent.Owner) > 0)
                Dirty(uid, collectiveMind);
        }
    }

    public bool TryCreateGroup(EntityUid group, ProtoId<CollectiveMindPrototype> mind, EntityUid? owner = null)
    {
        if (!Exists(group) || owner is { } groupOwner && !Exists(groupOwner))
            return false;

        if (!_prototype.Resolve(mind, out var prototype) || prototype.Mode != CollectiveMindMode.Group)
            return false;

        if (!TryComp<CollectiveMindGroupComponent>(group, out var collectiveMindGroup))
        {
            AddComp(group, new CollectiveMindGroupComponent
            {
                Mind = mind,
                GroupOwner = owner,
            });
            return true;
        }

        if (collectiveMindGroup.Mind != mind)
            return false;

        collectiveMindGroup.GroupOwner = owner ?? collectiveMindGroup.GroupOwner;
        return true;
    }

    public bool TryRemoveGroup(Entity<CollectiveMindGroupComponent?> group)
    {
        if (!Resolve(group, ref group.Comp, false))
            return false;

        RemComp(group, group.Comp);
        return true;
    }

    public bool TrySetGroupOwner(Entity<CollectiveMindGroupComponent?> group, EntityUid? owner)
    {
        if (!Resolve(group, ref group.Comp, false) || owner is { } groupOwner && !Exists(groupOwner))
            return false;

        group.Comp.GroupOwner = owner;
        return true;
    }

    public bool TryGetGroupOwner(Entity<CollectiveMindGroupComponent?> group, out EntityUid? owner)
    {
        owner = null;
        if (!Resolve(group, ref group.Comp, false))
            return false;

        owner = group.Comp.GroupOwner;
        return true;
    }

    #endregion

    #region Membership management

    public bool TryAddMember(EntityUid member, ProtoId<CollectiveMindPrototype> mind, EntityUid? group = null, CollectiveMindPermissions permissions = CollectiveMindPermissions.Full)
    {
        if (!Exists(member) || !TryGetMembershipGroupId(mind, group, out var groupId))
            return false;

        var collectiveMind = EnsureComp<CollectiveMindComponent>(member);
        SetMembership((member, collectiveMind), mind, groupId, permissions);

        return true;
    }

    public bool TryRemoveMember(Entity<CollectiveMindComponent?> member, ProtoId<CollectiveMindPrototype> mind, EntityUid? group = null)
    {
        if (!Resolve(member, ref member.Comp, false) ||
            !TryFindMembership(member.Comp, mind, group, out var index))
            return false;

        member.Comp.Memberships.RemoveAt(index);
        Dirty(member);
        return true;
    }

    public bool TrySetMemberPermissions(Entity<CollectiveMindComponent?> member, ProtoId<CollectiveMindPrototype> mind, CollectiveMindPermissions permissions, EntityUid? group = null)
    {
        if (!Resolve(member, ref member.Comp, false) ||
            !TryFindMembership(member.Comp, mind, group, out var index))
            return false;

        member.Comp.Memberships[index] = member.Comp.Memberships[index] with { Permissions = permissions };

        Dirty(member);
        return true;
    }

    #endregion

    #region Message routing

    public bool TryResolveSender(Entity<CollectiveMindComponent?> member, ProtoId<CollectiveMindPrototype> mind, out EntityUid? group)
    {
        group = null;
        return Resolve(member, ref member.Comp, false) &&
               TryResolveSender(member.Comp, mind, out group);
    }

    public bool TryResolveSender(Entity<CollectiveMindComponent?> member, CollectiveMindPrototype mind, out EntityUid? group)
    {
        group = null;
        return Resolve(member, ref member.Comp, false) &&
               TryResolveSender(member.Comp, mind, out group);
    }

    public bool CanReceive(Entity<CollectiveMindComponent> member, CollectiveMindPrototype mind, EntityUid? group)
    {
        if (mind.Mode == CollectiveMindMode.Group && group is null)
            return false;

        var groupId = mind.Mode == CollectiveMindMode.Global ? null : group;
        return HasMembershipPermission(member.Comp, mind.ID, groupId, CollectiveMindPermissions.Receive);
    }

    public bool TryGetConfiguredMind(Entity<CollectiveMindComponent?> member, out ProtoId<CollectiveMindPrototype> mind)
    {
        mind = default;
        if (!Resolve(member, ref member.Comp, false) || member.Comp.DefaultMind is not { } defaultMind)
            return false;

        if (!_prototype.HasIndex(defaultMind))
            return false;

        mind = defaultMind;
        return true;
    }

    public bool TryGetDefaultMind(Entity<CollectiveMindComponent?> member, out ProtoId<CollectiveMindPrototype> mind)
    {
        mind = default;
        return Resolve(member, ref member.Comp, false) &&
               TryGetDefaultMind(member.Comp, out mind);
    }

    public bool TryGetRedirectedMind(Entity<CollectiveMindComponent?> member, out ProtoId<CollectiveMindPrototype> mind)
    {
        mind = default;
        if (!Resolve(member, ref member.Comp, false) || !member.Comp.RedirectSpeech)
            return false;

        return TryGetDefaultMind(member.Comp, out mind);
    }

    #endregion

    #region Helpers

    private bool TryGetDefaultMind(CollectiveMindComponent collectiveMind, out ProtoId<CollectiveMindPrototype> mind)
    {
        mind = default;
        if (collectiveMind.DefaultMind is { } defaultMind && TryResolveSender(collectiveMind, defaultMind, out _))
        {
            mind = defaultMind;
            return true;
        }

        var found = false;
        // Без DefaultMind канал выбирается автоматически, только если доступен ровно один кол разум
        foreach (var membership in collectiveMind.Memberships)
        {
            if (!TryResolveSender(collectiveMind, membership.Mind, out _))
                continue;

            if (found && mind != membership.Mind)
                return false;

            mind = membership.Mind;
            found = true;
        }

        return found;
    }

    private bool TryResolveSender(CollectiveMindComponent collectiveMind, ProtoId<CollectiveMindPrototype> mind, out EntityUid? group)
    {
        group = null;
        return _prototype.Resolve(mind, out var prototype) && TryResolveSender(collectiveMind, prototype, out group);
    }

    private bool TryResolveSender(CollectiveMindComponent collectiveMind, CollectiveMindPrototype prototype, out EntityUid? group)
    {
        group = null;
        if (prototype.Mode == CollectiveMindMode.Global)
            return HasMembershipPermission(collectiveMind, prototype.ID, null, CollectiveMindPermissions.Send);

        var index = FindMembership(collectiveMind, prototype.ID);
        if (index < 0)
            return false;

        var membership = collectiveMind.Memberships[index];
        if (membership.Group is not { } groupId || (membership.Permissions & CollectiveMindPermissions.Send) == 0)
            return false;

        if (!TryComp<CollectiveMindGroupComponent>(groupId, out var collectiveMindGroup) ||
            collectiveMindGroup.Mind != prototype.ID)
            return false;

        group = groupId;
        return true;
    }

    private void SetMembership(Entity<CollectiveMindComponent> member, ProtoId<CollectiveMindPrototype> mind, EntityUid? group, CollectiveMindPermissions permissions)
    {
        var membership = new CollectiveMindMembership
        {
            Mind = mind,
            Group = group,
            Permissions = permissions,
        };

        // Участник может состоять только в одной группе каждого типа разума.
        var index = FindMembership(member.Comp, mind);

        if (index < 0)
            member.Comp.Memberships.Add(membership);
        else
            member.Comp.Memberships[index] = membership;

        Dirty(member);
    }

    private bool TryFindMembership(CollectiveMindComponent collectiveMind, ProtoId<CollectiveMindPrototype> mind, EntityUid? group, out int index)
    {
        index = -1;
        if (!TryGetMembershipGroupId(mind, group, out var groupId))
            return false;

        index = FindMembership(collectiveMind, mind, groupId);
        return index >= 0;
    }

    private bool TryGetMembershipGroupId(ProtoId<CollectiveMindPrototype> mind, EntityUid? group, out EntityUid? groupId)
    {
        groupId = null;
        if (!_prototype.Resolve(mind, out var prototype))
            return false;

        if (prototype.Mode == CollectiveMindMode.Global)
            return group is null;

        if (group is not { } groupEntity || !TryComp<CollectiveMindGroupComponent>(groupEntity, out var collectiveMindGroup) || collectiveMindGroup.Mind != mind)
            return false;

        groupId = groupEntity;
        return true;
    }

    private static int FindMembership(CollectiveMindComponent component, ProtoId<CollectiveMindPrototype> mind, EntityUid? group)
    {
        for (var i = 0; i < component.Memberships.Count; i++)
        {
            var membership = component.Memberships[i];
            if (membership.Mind == mind && membership.Group == group)
                return i;
        }

        return -1;
    }

    private static int FindMembership(CollectiveMindComponent component, ProtoId<CollectiveMindPrototype> mind)
    {
        for (var i = 0; i < component.Memberships.Count; i++)
        {
            if (component.Memberships[i].Mind == mind)
                return i;
        }

        return -1;
    }

    private static bool HasMembershipPermission(CollectiveMindComponent component, ProtoId<CollectiveMindPrototype> mind, EntityUid? group, CollectiveMindPermissions permission)
    {
        foreach (var membership in component.Memberships)
        {
            if (membership.Mind == mind && membership.Group == group)
                return (membership.Permissions & permission) != 0;
        }

        return false;
    }

    #endregion
}
