using Content.Server.CartridgeLoader;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared._Sunrise.Laws;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CartridgeLoader.Cartridges;

public sealed class CorporateLawCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CorporateLawCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(Entity<CorporateLawCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        var lawsetId = _config.GetCVar(SunriseCCVars.CorporateLawSet);
        if (!_prototype.TryIndex<CorporateLawsetPrototype>(lawsetId, out var lawset))
            return;

        var sections = new List<LawSection>();

        // 1. General Provisions
        if (lawset.Provisions.Count > 0)
        {
            var provisionEntries = new List<LawEntry>();
            foreach (var entryId in lawset.Provisions)
            {
                if (!_prototype.TryIndex(entryId, out var entry))
                    continue;

                provisionEntries.Add(new LawEntry(entry.LawIdentifier, Loc.GetString(entry.Title), Loc.GetString(entry.Description)));
            }

            sections.Add(new LawSection(Loc.GetString("sunrise-records-provisions-header"), null, provisionEntries));
        }

        // 2. Legal Articles (Categorized)
        foreach (var sectionId in lawset.Articles)
        {
            if (!_prototype.TryIndex(sectionId, out var section))
                continue;

            var entries = new List<LawEntry>();
            foreach (var entryId in section.Entries)
            {
                if (!_prototype.TryIndex(entryId, out var entry) || entry.Category == LawCategory.Provision)
                    continue;

                entries.Add(new LawEntry(entry.LawIdentifier, Loc.GetString(entry.Title), Loc.GetString(entry.Description)));
            }

            sections.Add(new LawSection(Loc.GetString(section.Title), section.Color, entries));
        }

        // 3. Modifiers (Circumstances)
        if (lawset.Circumstances.Count > 0)
        {
            var mitEntries = new List<LawEntry>();
            var aggEntries = new List<LawEntry>();

            foreach (var entryId in lawset.Circumstances)
            {
                if (!_prototype.TryIndex(entryId, out var entry) || entry.Category == LawCategory.Provision)
                    continue;

                var lawEntry = new LawEntry(entry.LawIdentifier, Loc.GetString(entry.Title), Loc.GetString(entry.Description));

                if (entry.Category == LawCategory.Mitigating)
                    mitEntries.Add(lawEntry);
                else if (entry.Category == LawCategory.Aggravating)
                    aggEntries.Add(lawEntry);
            }

            if (mitEntries.Count > 0)
            {
                sections.Add(new LawSection(Loc.GetString("sunrise-records-mitigating-circumstances"), Color.FromHex("#00ff9d"), mitEntries));
            }

            if (aggEntries.Count > 0)
            {
                sections.Add(new LawSection(Loc.GetString("sunrise-records-aggravating-circumstances"), Color.FromHex("#ff4d4d"), aggEntries));
            }
        }

        var state = new CorporateLawUiState(sections);
        _cartridgeLoader.UpdateCartridgeUiState(args.Loader, state);
    }
}
