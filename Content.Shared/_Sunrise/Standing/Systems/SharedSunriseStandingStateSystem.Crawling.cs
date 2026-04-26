using Content.Shared._Sunrise.Standing.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Standing.Systems;

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
        return _tag.HasTag(ent.Owner, FootstepSoundTag);
    }

    private void ApplyCrawlingFootstepModifier(Entity<CrawlerComponent> ent)
    {
        var crawlingFootstep = EnsureComp<CrawlingFootstepModifierComponent>(ent.Owner);
        crawlingFootstep.HadFootstepSoundTag = true;
        _tag.RemoveTag(ent.Owner, FootstepSoundTag);
    }

    private bool TryRestoreCrawlingFootstepModifier(Entity<CrawlingFootstepModifierComponent> ent)
    {
        RestoreCrawlingFootstepModifier(ent);
        return true;
    }

    private void RestoreCrawlingFootstepModifier(Entity<CrawlingFootstepModifierComponent> ent)
    {
        if (ent.Comp.HadFootstepSoundTag &&
            TryComp<TagComponent>(ent.Owner, out var tagComp) &&
            !_tag.HasTag(tagComp, FootstepSoundTag))
        {
            _tag.AddTag((ent.Owner, tagComp), FootstepSoundTag);
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
