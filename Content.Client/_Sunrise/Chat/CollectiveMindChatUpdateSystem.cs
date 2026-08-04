using Content.Client.Chat.Managers;
using Content.Shared._Sunrise.CollectiveMind;
using Robust.Client.Player;

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
        SubscribeLocalEvent<CollectiveMindComponent, AfterAutoHandleStateEvent>(OnStateChanged);
    }

    private void OnInit(Entity<CollectiveMindComponent> ent, ref ComponentInit args)
        => UpdatePermissions(ent);

    private void OnRemove(Entity<CollectiveMindComponent> ent, ref ComponentRemove args)
        => UpdatePermissions(ent, true);

    private void OnStateChanged(Entity<CollectiveMindComponent> ent, ref AfterAutoHandleStateEvent args)
        => UpdatePermissions(ent);

    private bool HasPermission(CollectiveMindPermissions permission)
    {
        if (_player.LocalEntity is not { } localEntity ||
            _removingEntity == localEntity ||
            !TryComp<CollectiveMindComponent>(localEntity, out var collectiveMind))
            return false;

        return (collectiveMind.ClientPermissions & permission) != 0;
    }

    private void UpdatePermissions(EntityUid uid, bool removing = false)
    {
        if (_player.LocalEntity != uid)
            return;

        _removingEntity = removing ? uid : null;
        _chat.UpdatePermissions();
    }
}
