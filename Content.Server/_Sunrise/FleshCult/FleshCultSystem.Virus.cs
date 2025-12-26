using Content.Server._Sunrise.FleshCult.GameRule;
using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.FleshCult;

public sealed partial class FleshCultSystem
{
    private void InitializeVirus()
    {
        SubscribeLocalEvent<PendingFleshCultistComponent, MapInitEvent>(OnPendingMapInit);
    }

    private void OnPendingMapInit(EntityUid uid, PendingFleshCultistComponent component, MapInitEvent args)
    {
        component.NextParalyze = _timing.CurTime + TimeSpan.FromSeconds(1f);
        component.NextScream = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }

    public void UpdateVirus(float frameTime)
    {
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<PendingFleshCultistComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentStage == PendingFleshCultistStage.Final)
                continue;

            comp.Accumulator += frameTime;

            var stageTimer = comp.CurrentStage switch
            {
                PendingFleshCultistStage.First => comp.FirstStageTimer,
                PendingFleshCultistStage.Second => comp.SecondStageTimer,
                _ => 0f
            };

            if (comp.Accumulator >= stageTimer)
            {
                comp.Accumulator = 0f;
                comp.CurrentStage = GetNextStage(comp.CurrentStage);
            }

            switch (comp.CurrentStage)
            {
                case PendingFleshCultistStage.First:
                    if (comp.NextScream <= curTime)
                    {
                        comp.NextScream = curTime + TimeSpan.FromSeconds(comp.ScreamInterval);
                        _chatSystem.TryEmoteWithChat(uid, "Scream");
                    }
                    if (comp.NextStutter <= curTime)
                    {
                        comp.NextStutter = curTime + TimeSpan.FromSeconds(comp.ScreamInterval);
                        _stuttering.DoStutter(uid, TimeSpan.FromSeconds(comp.StutterTime), true);
                    }
                    break;
                case PendingFleshCultistStage.Second:
                    if (comp.NextParalyze <= curTime)
                    {
                        comp.NextParalyze = curTime + TimeSpan.FromSeconds(comp.ParalyzeInterval);
                        _stunSystem.TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(comp.ParalyzeTime));
                    }
                    if (comp.NextJitter <= curTime)
                    {
                        comp.NextJitter = curTime + TimeSpan.FromSeconds(comp.JitterInterval);
                        _jittering.DoJitter(uid, TimeSpan.FromSeconds(comp.JitterTime), true);
                    }
                    break;
                case PendingFleshCultistStage.Third:
                {
                    if (!HasComp<MindContainerComponent>(uid) || !TryComp<ActorComponent>(uid, out var targetActor))
                        return;

                    var targetPlayer = targetActor.PlayerSession;

                    if (HasComp<MindShieldComponent>(uid))
                    {
                        // SUNRISE-TODO: Сделать это внутри системы майншилда
                        _popup.PopupEntity("Активация самоуничтожения импланта защиты разума", uid, PopupType.LargeCaution);
                        _body.GibBody(uid, true);
                        _explosionSystem.QueueExplosion(uid, "Default", 50, 5, 30, canCreateVacuum: false);
                        break;
                    }

                    if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
                        break;

                    if (!_speciesWhitelist.Contains(humanoidAppearance.Species))
                    {
                        RemCompDeferred<PendingFleshCultistComponent>(uid);
                        break;
                    }

                    _antag.ForceMakeAntag<FleshCultRuleComponent>(targetPlayer, DefaultFleshCultRule);

                    comp.CurrentStage = PendingFleshCultistStage.Final;
                    RemCompDeferred<PendingFleshCultistComponent>(uid);
                    break;
                }
            }
        }
    }

    private PendingFleshCultistStage GetNextStage(PendingFleshCultistStage currentStage)
    {
        return currentStage switch
        {
            PendingFleshCultistStage.First => PendingFleshCultistStage.Second,
            PendingFleshCultistStage.Second => PendingFleshCultistStage.Third,
            PendingFleshCultistStage.Third => PendingFleshCultistStage.Final,
            _ => currentStage,
        };
    }
}
