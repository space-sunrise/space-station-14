using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.Flip;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Sunrise.Flip;

public sealed class FlipSystem : SharedFlipSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    [ValidatePrototypeId<EmotePrototype>]
    private const string EmoteFlipProto = "Flip";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlipOnAttackComponent, MeleeHitEvent>(OnFlipOnAttack);
    }

    private void OnFlipOnAttack(EntityUid uid, FlipOnAttackComponent component, MeleeHitEvent args)
    {
        Logger.Info("OnFlipOnAttack");
        foreach (var entity in args.HitEntities)
        {
            if (!HasComp<MobStateComponent>(entity))
                continue;

            PlayEmoteFlip(args.User);
            return;
        }
    }

    private void PlayEmoteFlip(EntityUid uid)
    {
        _chat.TryEmoteWithChat(uid, EmoteFlipProto);
    }
}
