using System.Threading.Tasks;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.NPC.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;

namespace Content.Server._Sunrise.FactionClothingBlockerSystem;

public sealed class FactionClothingBlockerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionClothingBlockerComponent, GotEquippedEvent>(OnGotEquipped);
    }

    private async void OnGotEquipped(EntityUid uid, FactionClothingBlockerComponent component, GotEquippedEvent args)
    {
        var canUse = false;
        if (TryComp<NpcFactionMemberComponent>(args.Equipee, out var npcFactionMemberComponent))
        {
            foreach (var faction in npcFactionMemberComponent.Factions)
            {
                if (component.Factions.Contains(faction))
                    canUse = true;
            }
        }

        if (canUse)
            return;

        EntityManager.EnsureComponent<UnremoveableComponent>(uid);
        await PopupWithDelays(uid, component);
        _bodySystem.GibBody(args.Equipee, true);
        _explosionSystem.QueueExplosion(uid, "Default", 50, 5, 30, canCreateVacuum: false);
    }

    private async Task PopupWithDelays(EntityUid uid, FactionClothingBlockerComponent component)
    {
        var notifications = new[]
        {
            new { Message = Loc.GetString("faction-clothing-blocker-notify-wrong-user-detected"), Delay = TimeSpan.FromSeconds(2), PopupType = PopupType.LargeCaution },
            new { Message = Loc.GetString("faction-clothing-blocker-notify-inclusion-bolts"), Delay = TimeSpan.FromSeconds(2), PopupType = PopupType.LargeCaution },
            new { Message = Loc.GetString("faction-clothing-blocker-notify-activate-self-destruction"), Delay = TimeSpan.FromSeconds(2), PopupType = PopupType.LargeCaution }
        };

        foreach (var notification in notifications)
        {

            _audioSystem.PlayPvs(component.BeepSound, uid);
            await PopupWithDelay(notification.Message, uid, notification.PopupType);
            await Task.Delay(notification.Delay);
        }

        for (int i = 10; i > 0; i--)
        {
            _audioSystem.PlayPvs(component.BeepSound, uid);
            await PopupWithDelay(i.ToString(), uid, PopupType.LargeCaution);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    private async Task PopupWithDelay(string message, EntityUid uid, PopupType popupType)
    {
        _popup.PopupEntity(message, uid, popupType);
    }
}
