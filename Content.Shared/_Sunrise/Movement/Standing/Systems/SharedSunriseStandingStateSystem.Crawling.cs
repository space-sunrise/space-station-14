using Content.Shared._Sunrise.Movement.Standing.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Movement.Standing.Systems;

public abstract partial class SharedSunriseStandingStateSystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> FootstepSoundTag = "FootstepSound";

    private void InitializeCrawlingFootstepModifier()
    {
        SubscribeLocalEvent<CrawlerComponent, DownedEvent>(OnCrawlingFootstepDowned);
        SubscribeLocalEvent<CrawlingFootstepModifierComponent, StoodEvent>(OnCrawlingFootstepStood);
    }

    private void OnCrawlingFootstepDowned(Entity<CrawlerComponent> ent, ref DownedEvent args)
    {
        TryApplyCrawlingFootstepModifier(ent);
    }

    private void OnCrawlingFootstepStood(Entity<CrawlingFootstepModifierComponent> ent, ref StoodEvent args)
    {
        RestoreCrawlingFootstepModifier(ent);
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
        return _tag.HasTag(ent, FootstepSoundTag);
    }

    private void ApplyCrawlingFootstepModifier(Entity<CrawlerComponent> ent)
    {
        var crawlingFootstep = EnsureComp<CrawlingFootstepModifierComponent>(ent);
        crawlingFootstep.HadFootstepSoundTag = true;
        _tag.RemoveTag(ent.Owner, FootstepSoundTag);

        Dirty(ent, crawlingFootstep);
    }

    private void RestoreCrawlingFootstepModifier(Entity<CrawlingFootstepModifierComponent> ent)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        if (ent.Comp.HadFootstepSoundTag && !_tag.HasTag(ent, FootstepSoundTag))
        {
            _tag.AddTag(ent, FootstepSoundTag);
        }

        RemCompDeferred<CrawlingFootstepModifierComponent>(ent);
    }

    public void RefreshProneCrawlVisualsMarker(Entity<StandingStateComponent> ent, bool willBeDowned)
    {
        if (!_crawlerQuery.HasComp(ent))
            return;

        if (willBeDowned)
            EnsureComp<ActiveProneCrawlVisualsComponent>(ent);
        else
            RemCompDeferred<ActiveProneCrawlVisualsComponent>(ent);
    }
}
