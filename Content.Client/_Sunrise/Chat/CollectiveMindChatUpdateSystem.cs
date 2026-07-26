using Content.Client.Chat.Managers;
using Content.Shared._Sunrise.CollectiveMind;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Chat;

public sealed class CollectiveMindChatUpdateSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityUid? _removingEntity;

    public bool CanSend => HasPermission(CollectiveMindPermissions.Send);
    public bool CanReceive => HasPermission(CollectiveMindPermissions.Receive);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollectiveMindComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CollectiveMindComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CollectiveMindComponent, ComponentHandleState>(OnStateChanged);
    }

    private void OnInit(Entity<CollectiveMindComponent> ent, ref ComponentInit args)
    {
        if (_removingEntity == ent.Owner)
            _removingEntity = null;

        UpdatePermissions(ent);
    }

    private void OnRemove(Entity<CollectiveMindComponent> ent, ref ComponentRemove args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _removingEntity = ent.Owner;
        _chat.UpdatePermissions();
    }

    private void OnStateChanged(Entity<CollectiveMindComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not CollectiveMindComponentState state)
            return;

        ent.Comp.Memberships.Clear();
        foreach (var membership in state.Memberships)
        {
            ent.Comp.Memberships.Add(new CollectiveMindMembership
            {
                Mind = membership.Mind,
                Permissions = membership.Permissions,
            });
        }

        if (_removingEntity == ent.Owner)
            _removingEntity = null;

        UpdatePermissions(ent);
    }

    private bool HasPermission(CollectiveMindPermissions permission)
    {
        if (_player.LocalEntity is not { } localEntity ||
            _removingEntity == localEntity ||
            !TryComp<CollectiveMindComponent>(localEntity, out var collectiveMind))
            return false;

        foreach (var membership in collectiveMind.Memberships)
        {
            if ((membership.Permissions & permission) != 0)
                return true;
        }

        return false;
    }

    private void UpdatePermissions(EntityUid uid)
    {
        if (_player.LocalEntity != uid)
            return;

        _chat.UpdatePermissions();
    }
}
