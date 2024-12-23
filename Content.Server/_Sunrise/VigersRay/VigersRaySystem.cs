using System.Numerics;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Jittering;
using Content.Server.Light.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stunnable;
using Content.Server.Traits.Assorted;
using Content.Shared.Administration.Components;
using Content.Shared.Clumsy;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Traits.Assorted;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.VigersRay;

public sealed class VigersRaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly CreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly ParacusiaSystem _paracusiaSystem = default!;
    [Dependency] private readonly NarcolepsySystem _narcolepsySystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly JitteringSystem _jittering = default!;
    [Dependency] private readonly StutteringSystem _stuttering = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VigersRayJoinEvent>(OnVigersRayJoin);
        SubscribeLocalEvent<VigersRayLeaveEvent>(OnVigersRayLeave);
    }

    private void OnVigersRayLeave(VigersRayLeaveEvent args)
    {
        _chatManager.DispatchServerAnnouncement("VigersRay ушел", Color.Green);

         // Увы
         //var audio = AudioParams.Default;
         //_audioSystem.PlayGlobal("/Audio/_Sunrise/leave.ogg", Filter.Broadcast(), false, audio.WithVolume(10));
    }

    private void OnVigersRayJoin(VigersRayJoinEvent args)
    {
        _chatManager.DispatchServerAnnouncement("VigersRay пришел", Color.Red);

        var blobFactoryQuery = EntityQueryEnumerator<PoweredLightComponent>();
        while (blobFactoryQuery.MoveNext(out var ent, out var comp))
        {
            var ghostBoo = new GhostBooEvent();
            RaiseLocalEvent(ent, ghostBoo, true);
        }
        var statusEffectQuery = EntityQueryEnumerator<StatusEffectsComponent>();
        while (statusEffectQuery.MoveNext(out var ent, out var comp))
        {
            _stunSystem.TryParalyze(ent, TimeSpan.FromSeconds(5), true);
            _jittering.DoJitter(ent, TimeSpan.FromSeconds(15), true);
            _stuttering.DoStutter(ent, TimeSpan.FromSeconds(30), true);
        }

        // Увы
        //var audio = AudioParams.Default;
        //_audioSystem.PlayGlobal("/Audio/_Sunrise/stab.ogg", Filter.Broadcast(), false, audio.WithVolume(10));
        //_audioSystem.PlayGlobal("/Audio/_Sunrise/night.ogg", Filter.Broadcast(), false, audio.WithVolume(10));
    }

    private const float CheckDelay = 10;
    private readonly List<string> _victims = new()
    {
        // Помилован
        // "Notmedic", // Менял имя своего госта на VigersRay и ставил скин бубльгума, а после летал пугал всех. Очень опрометчивое решение.
        // Помилован
    };
    private TimeSpan _checkTime;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker.RunLevel != GameRunLevel.InRound)
        {
            return;
        }

        if (_timing.CurTime < _checkTime)
            return;

        _checkTime = _timing.CurTime + TimeSpan.FromSeconds(CheckDelay);

        foreach (var pSession in Filter.GetAllPlayers())
        {
            if (pSession.Status != SessionStatus.InGame)
                continue;

            if (!_victims.Contains(pSession.Data.UserName))
                continue;

            if (pSession.AttachedEntity == null)
                continue;

            EnsureComp<ClumsyComponent>(pSession.AttachedEntity.Value);
            EnsureComp<OwOAccentComponent>(pSession.AttachedEntity.Value);
            EnsureComp<LightweightDrunkComponent>(pSession.AttachedEntity.Value);
            EnsureComp<StutteringAccentComponent>(pSession.AttachedEntity.Value);
            var paracusia = EnsureComp<ParacusiaComponent>(pSession.AttachedEntity.Value);
            _paracusiaSystem.SetSounds(pSession.AttachedEntity.Value, new SoundCollectionSpecifier("Paracusia"), paracusia);
            _paracusiaSystem.SetTime(pSession.AttachedEntity.Value, 0.1f, 300, paracusia);
            _paracusiaSystem.SetDistance(pSession.AttachedEntity.Value, 7f, paracusia);
            var narcolepsy = EnsureComp<NarcolepsyComponent>(pSession.AttachedEntity.Value);
            _narcolepsySystem.SetTime(pSession.AttachedEntity.Value, new Vector2(300, 600), new Vector2(10, 30), narcolepsy);
            EnsureComp<FrontalLispComponent>(pSession.AttachedEntity.Value);
            EnsureComp<DisarmProneComponent>(pSession.AttachedEntity.Value);
            if (TryComp<CreamPiedComponent>(pSession.AttachedEntity.Value, out var creamPied))
                _creamPieSystem.SetCreamPied(pSession.AttachedEntity.Value, creamPied, true);
            var stamina = EnsureComp<StaminaComponent>(pSession.AttachedEntity.Value);
            stamina.CritThreshold = 1;
            var metadata = MetaData(pSession.AttachedEntity.Value);
            if (metadata.EntityPrototype != null && metadata.EntityPrototype.ID != "MobMonkey")
                _polymorphSystem.PolymorphEntity(pSession.AttachedEntity.Value, "PermanentlyMonkey");
        }
    }
}

public sealed class VigersRayJoinEvent : EntityEventArgs
{
}

public sealed class VigersRayLeaveEvent : EntityEventArgs
{
}
