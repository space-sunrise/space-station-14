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
    [Dependency] private readonly IConfigurationManager _config = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CorporateLawCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, CorporateLawCartridgeComponent component, CartridgeUiReadyEvent args)
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
                if (!_prototype.TryIndex<CorporateLawPrototype>(entryId, out var entry))
                    continue;

                provisionEntries.Add(new LawEntry
                {
                    Identifier = entry.LawIdentifier,
                    Title = Loc.GetString(entry.Title),
                    Description = Loc.GetString(entry.Description)
                });
            }

            sections.Add(new LawSection
            {
                Title = Loc.GetString("sunrise-records-provisions-header"),
                Entries = provisionEntries
            });
        }

        // 2. Legal Articles (Categorized)
        foreach (var sectionId in lawset.Articles)
        {
            if (!_prototype.TryIndex<CorporateLawSectionPrototype>(sectionId, out var section))
                continue;

            var entries = new List<LawEntry>();
            foreach (var entryId in section.Entries)
            {
                if (!_prototype.TryIndex<CorporateLawPrototype>(entryId, out var entry) || entry.Category == LawCategory.Provision)
                    continue;

                entries.Add(new LawEntry
                {
                    Identifier = entry.LawIdentifier,
                    Title = Loc.GetString(entry.Title),
                    Description = Loc.GetString(entry.Description)
                });
            }

            sections.Add(new LawSection
            {
                Title = Loc.GetString(section.Title),
                Color = section.Color,
                Entries = entries
            });
        }

        // 3. Modifiers (Circumstances)
        if (lawset.Circumstances.Count > 0)
        {
            var mitEntries = new List<LawEntry>();
            var aggEntries = new List<LawEntry>();

            foreach (var entryId in lawset.Circumstances)
            {
                if (!_prototype.TryIndex<CorporateLawPrototype>(entryId, out var entry) || entry.Category == LawCategory.Provision)
                    continue;

                var lawEntry = new LawEntry
                {
                    Identifier = entry.LawIdentifier,
                    Title = Loc.GetString(entry.Title),
                    Description = Loc.GetString(entry.Description)
                };

                if (entry.Category == LawCategory.Mitigating)
                    mitEntries.Add(lawEntry);
                else if (entry.Category == LawCategory.Aggravating)
                    aggEntries.Add(lawEntry);
            }

            if (mitEntries.Count > 0)
            {
                sections.Add(new LawSection
                {
                    Title = Loc.GetString("sunrise-records-mitigating-circumstances"),
                    Color = Color.FromHex("#00ff9d"),
                    Entries = mitEntries
                });
            }

            if (aggEntries.Count > 0)
            {
                sections.Add(new LawSection
                {
                    Title = Loc.GetString("sunrise-records-aggravating-circumstances"),
                    Color = Color.FromHex("#ff4d4d"),
                    Entries = aggEntries
                });
            }
        }

        var state = new CorporateLawUiState(sections);
        _cartridgeLoader.UpdateCartridgeUiState(args.Loader, state);
    }
}
