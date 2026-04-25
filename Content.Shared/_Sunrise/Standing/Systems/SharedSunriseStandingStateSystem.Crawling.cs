using Content.Shared._Sunrise.Standing.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared._Sunrise.Standing.Systems;

public abstract partial class SharedSunriseStandingStateSystem
{
    private EntityQuery<FootstepModifierComponent> _footstepModifierQuery;

    private void InitializeCrawlingFootstepModifier()
    {
        _footstepModifierQuery = GetEntityQuery<FootstepModifierComponent>();

        SubscribeLocalEvent<CrawlerComponent, DownedEvent>(OnCrawlingFootstepDowned);
        SubscribeLocalEvent<CrawlingFootstepModifierComponent, StoodEvent>(OnCrawlingFootstepStood);
    }

    private void OnCrawlingFootstepDowned(Entity<CrawlerComponent> ent, ref DownedEvent args)
    {
        TryApplyCrawlingFootstepModifier(ent);
    }

    private void OnCrawlingFootstepStood(Entity<CrawlingFootstepModifierComponent> ent, ref StoodEvent args)
    {
        TryRestoreCrawlingFootstepModifier(ent);
    }

    private bool TryApplyCrawlingFootstepModifier(Entity<CrawlerComponent> ent)
    {
        if (!CanApplyCrawlingFootstepModifier(ent))
            return false;

        ApplyCrawlingFootstepModifier(ent);
        return true;
    }

    private bool CanApplyCrawlingFootstepModifier(Entity<CrawlerComponent> ent)
    {
        return ent.Comp.CrawlingSound != null;
    }

    private void ApplyCrawlingFootstepModifier(Entity<CrawlerComponent> ent)
    {
        var hadFootstepModifier = _footstepModifierQuery.TryComp(ent.Owner, out var footstepModifier);

        if (!TryComp<CrawlingFootstepModifierComponent>(ent.Owner, out var crawlingFootstep))
        {
            crawlingFootstep = EnsureComp<CrawlingFootstepModifierComponent>(ent.Owner);
            crawlingFootstep.HadFootstepModifier = hadFootstepModifier;
            crawlingFootstep.OriginalSound = hadFootstepModifier
                ? footstepModifier?.FootstepSoundCollection
                : null;
        }

        if (!hadFootstepModifier)
            footstepModifier = EnsureComp<FootstepModifierComponent>(ent.Owner);

        crawlingFootstep.AppliedSound = ent.Comp.CrawlingSound;
        footstepModifier!.FootstepSoundCollection = ent.Comp.CrawlingSound;
        Dirty(ent.Owner, footstepModifier);
    }

    private bool TryRestoreCrawlingFootstepModifier(Entity<CrawlingFootstepModifierComponent> ent)
    {
        RestoreCrawlingFootstepModifier(ent);
        return true;
    }

    private void RestoreCrawlingFootstepModifier(Entity<CrawlingFootstepModifierComponent> ent)
    {
        if (_footstepModifierQuery.TryComp(ent.Owner, out var footstepModifier) &&
            Equals(footstepModifier.FootstepSoundCollection, ent.Comp.AppliedSound))
        {
            if (ent.Comp.HadFootstepModifier)
            {
                footstepModifier.FootstepSoundCollection = ent.Comp.OriginalSound;
                Dirty(ent.Owner, footstepModifier);
            }
            else
            {
                RemComp<FootstepModifierComponent>(ent.Owner);
            }
        }

        RemCompDeferred<CrawlingFootstepModifierComponent>(ent.Owner);
    }

    public void RefreshProneCrawlVisualsMarker(Entity<StandingStateComponent> ent, bool willBeDowned)
    {
        if (willBeDowned)
            EnsureComp<ActiveProneCrawlVisualsComponent>(ent);
        else
            RemCompDeferred<ActiveProneCrawlVisualsComponent>(ent);
    }
}
